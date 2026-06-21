using System.Buffers;
using System.IO.Ports;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Transports.Options;

namespace SelfCheckout.HardwareBridge.Transports.Serial;

/// <summary>
/// <see cref="ITransport"/> implementation over <see cref="SerialPort"/>. Uses an async read pump
/// (<see cref="Task.Run(System.Action)"/>) that drains <see cref="SerialPort.BaseStream"/> into a bounded
/// <see cref="Channel{T}"/> consumers iterate via <see cref="ReadAllAsync"/>. Lifecycle transitions
/// (Open/Close/DisposeAsync) are guarded by a <see cref="SemaphoreSlim"/> so concurrent callers are serialized.
/// </summary>
public sealed class SerialTransport : ITransport
{
    private readonly SerialTransportOptions _options;
    private readonly ILogger<SerialTransport> _logger;
    private readonly Channel<ReadOnlyMemory<byte>> _channel;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private SerialPort? _port;
    private CancellationTokenSource? _readPumpCts;
    private Task? _readPumpTask;
    private bool _opened;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="SerialTransport"/>.
    /// </summary>
    /// <param name="options">Serial-port configuration. Required.</param>
    /// <param name="logger">Optional logger. Defaults to <see cref="NullLogger{T}.Instance"/>.</param>
    public SerialTransport(SerialTransportOptions options, ILogger<SerialTransport>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<SerialTransport>.Instance;
        _channel = Channel.CreateBounded<ReadOnlyMemory<byte>>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = true,
        });
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lifecycleLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_opened) return;

            var port = new SerialPort(_options.PortName, _options.BaudRate, _options.Parity, _options.DataBits, _options.StopBits)
            {
                Handshake = _options.Handshake,
                ReadTimeout = (int)_options.ReadTimeout.TotalMilliseconds,
                WriteTimeout = (int)_options.WriteTimeout.TotalMilliseconds,
                ReadBufferSize = _options.ReadBufferSize,
            };

            try
            {
                port.Open();
            }
            catch
            {
                port.Dispose();
                throw;
            }

            _port = port;
            _readPumpCts = new CancellationTokenSource();
            var pumpCt = _readPumpCts.Token;
            _readPumpTask = Task.Run(() => ReadPumpAsync(pumpCt), CancellationToken.None);
            _opened = true;

            _logger.LogDebug("SerialTransport opened on {PortName} @ {BaudRate} baud", _options.PortName, _options.BaudRate);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken ct = default)
    {
        await _lifecycleLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!_opened) return;

            try
            {
                _readPumpCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CTS already disposed; nothing to cancel.
            }

            if (_readPumpTask is not null)
            {
                try
                {
                    await _readPumpTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when pump exits via cancellation.
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SerialTransport read pump terminated with exception");
                }
            }

            _readPumpCts?.Dispose();
            _readPumpCts = null;
            _readPumpTask = null;

            try
            {
                _port?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SerialTransport: error closing serial port");
            }

            _port?.Dispose();
            _port = null;

            _channel.Writer.TryComplete();

            _opened = false;
            _logger.LogDebug("SerialTransport closed");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var port = _port;
        if (port is null || !port.IsOpen)
        {
            throw new InvalidOperationException("Transport not opened.");
        }

        await port.BaseStream.WriteAsync(data, ct).ConfigureAwait(false);
        await port.BaseStream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await CloseAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SerialTransport: error during DisposeAsync");
        }

        _lifecycleLock.Dispose();
    }

    private async Task ReadPumpAsync(CancellationToken pumpCt)
    {
        try
        {
            while (!pumpCt.IsCancellationRequested)
            {
                var port = _port;
                if (port is null || !port.IsOpen) break;

                var buffer = ArrayPool<byte>.Shared.Rent(_options.ReadBufferSize);
                try
                {
                    int read;
                    try
                    {
                        read = await port.BaseStream
                            .ReadAsync(buffer.AsMemory(0, _options.ReadBufferSize), pumpCt)
                            .ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    {
                        // SerialPort.ReadTimeout elapsed; just loop again.
                        continue;
                    }

                    if (read > 0)
                    {
                        var copy = new byte[read];
                        Buffer.BlockCopy(buffer, 0, copy, 0, read);
                        await _channel.Writer.WriteAsync(copy, pumpCt).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(10, pumpCt).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected shutdown path.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SerialTransport read pump failed");
            _channel.Writer.TryComplete(ex);
            throw;
        }
        finally
        {
            _channel.Writer.TryComplete();
        }
    }
}
