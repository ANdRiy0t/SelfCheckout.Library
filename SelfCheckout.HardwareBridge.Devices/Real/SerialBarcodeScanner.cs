using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Real.Options;
using SelfCheckout.HardwareBridge.Transports.Options;
using SelfCheckout.HardwareBridge.Transports.Serial;

namespace SelfCheckout.HardwareBridge.Devices.Real;

/// <summary>
/// Real <see cref="IBarcodeScanner"/> over a serial/USB-Serial port using <see cref="SerialTransport"/>.
/// A background read loop accumulates raw bytes until a configured line terminator (CR/LF by default),
/// decodes the barcode, and publishes it both as a <see cref="BarcodeScanned"/> event (push) and into an
/// internal channel consumed by <see cref="ScanAsync"/> (pull). Covers the bare CR/LF framing used by the
/// vast majority of RS-232 scanners; no STX/ETX framing is required.
/// </summary>
public class SerialBarcodeScanner : DeviceBase, IBarcodeScanner
{
    private readonly SerialTransport _transport;
    private readonly SerialScannerOptions _options;
    private readonly ILogger<SerialBarcodeScanner> _logger;
    private readonly Channel<ScanResult> _channel = Channel.CreateUnbounded<ScanResult>();
    private readonly List<byte> _accumulator = new();

    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;

    /// <inheritdoc />
    public event EventHandler<ScanResult>? BarcodeScanned;

    /// <summary>
    /// Initializes a new <see cref="SerialBarcodeScanner"/>.
    /// </summary>
    /// <param name="descriptor">Identity and connection metadata for this device.</param>
    /// <param name="transportOptions">Serial-port configuration used to open the underlying transport.</param>
    /// <param name="options">Scanner framing/symbology options. Defaults applied when null.</param>
    /// <param name="logger">Optional logger. Defaults to <see cref="NullLogger{T}.Instance"/>.</param>
    public SerialBarcodeScanner(
        DeviceDescriptor descriptor,
        SerialTransportOptions transportOptions,
        SerialScannerOptions? options = null,
        ILogger<SerialBarcodeScanner>? logger = null)
        : base(descriptor)
    {
        ArgumentNullException.ThrowIfNull(transportOptions);
        _transport = new SerialTransport(transportOptions, null);
        _options = options ?? new SerialScannerOptions();
        _logger = logger ?? NullLogger<SerialBarcodeScanner>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await _transport.OpenAsync(cancellationToken);
        _readLoopCts = new CancellationTokenSource();
        var token = _readLoopCts.Token;
        _readLoopTask = Task.Run(() => ReadLoopAsync(token), CancellationToken.None);
    }

    /// <inheritdoc />
    protected override async Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            _readLoopCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed; nothing to cancel.
        }

        if (_readLoopTask is not null)
        {
            try
            {
                await _readLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected shutdown path.
            }
        }

        _readLoopTask = null;
        _readLoopCts?.Dispose();
        _readLoopCts = null;

        await _transport.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        if (DeviceStatus != DeviceState.Ready)
        {
            throw new DeviceException(
                $"Scanner '{Descriptor.Id}' is not ready. Current state: {DeviceStatus}.",
                Descriptor.Id,
                Descriptor.Type,
                ErrorCode.ConnectionFailed);
        }

        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (var chunk in _transport.ReadAllAsync(token))
            {
                var span = chunk.Span;
                for (var i = 0; i < span.Length; i++)
                {
                    var b = span[i];
                    if (IsTerminator(b))
                    {
                        FlushLine();
                    }
                    else
                    {
                        _accumulator.Add(b);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SerialBarcodeScanner read loop terminated with exception");
        }
    }

    private bool IsTerminator(byte b)
    {
        foreach (var t in _options.LineTerminators)
        {
            if (b == (byte)t) return true;
        }

        return false;
    }

    private void FlushLine()
    {
        if (_accumulator.Count == 0)
        {
            // Empty line (e.g. the LF of a CRLF pair) — nothing to emit.
            return;
        }

        var barcode = _options.Encoding.GetString(_accumulator.ToArray()).Trim();
        _accumulator.Clear();

        if (string.IsNullOrEmpty(barcode))
        {
            return;
        }

        var result = new ScanResult(barcode, _options.DefaultSymbology, DateTimeOffset.UtcNow);
        BarcodeScanned?.Invoke(this, result);
        _channel.Writer.TryWrite(result);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _readLoopCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed.
            }

            _readLoopCts?.Dispose();
            _transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }
}
