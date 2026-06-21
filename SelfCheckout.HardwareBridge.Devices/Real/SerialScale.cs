using System.Globalization;
using System.Text.RegularExpressions;
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
/// Real <see cref="IWeightScale"/> over a serial/USB-Serial port using <see cref="SerialTransport"/>.
/// On connect the scale auto-detects its protocol: it sends the configured weight command and waits up to
/// <see cref="SerialScaleOptions.AutoDetectTimeout"/> for a reply. A reply selects command mode (request/response,
/// e.g. Massa-K / A&amp;D); otherwise it falls back to continuous mode (free-running stream, e.g. CAS / budget scales).
/// A background read loop parses every decoded line with a regex and caches the latest <see cref="WeightReading"/>.
/// </summary>
public class SerialScale : DeviceBase, IWeightScale
{
    private readonly SerialTransport _transport;
    private readonly SerialScaleOptions _options;
    private readonly ILogger<SerialScale> _logger;
    private readonly Regex _weightRegex;
    private readonly List<byte> _accumulator = new();
    private readonly object _latestLock = new();

    private WeightReading? _latest;
    private CancellationTokenSource? _readLoopCts;
    private Task? _readLoopTask;
    private bool _commandMode;
    private decimal _tareOffset;

    /// <summary>
    /// Initializes a new <see cref="SerialScale"/>.
    /// </summary>
    /// <param name="descriptor">Identity and connection metadata for this device.</param>
    /// <param name="transportOptions">Serial-port configuration used to open the underlying transport.</param>
    /// <param name="options">Scale protocol/parsing options. Defaults applied when null.</param>
    /// <param name="logger">Optional logger. Defaults to <see cref="NullLogger{T}.Instance"/>.</param>
    public SerialScale(
        DeviceDescriptor descriptor,
        SerialTransportOptions transportOptions,
        SerialScaleOptions? options = null,
        ILogger<SerialScale>? logger = null)
        : base(descriptor)
    {
        ArgumentNullException.ThrowIfNull(transportOptions);
        _transport = new SerialTransport(transportOptions, null);
        _options = options ?? new SerialScaleOptions();
        _logger = logger ?? NullLogger<SerialScale>.Instance;
        _weightRegex = new Regex(_options.WeightPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await _transport.OpenAsync(cancellationToken);
        _readLoopCts = new CancellationTokenSource();
        var token = _readLoopCts.Token;
        _readLoopTask = Task.Run(() => ReadLoopAsync(token), CancellationToken.None);

        // Auto-detect: clear any stale reading, send the weight command, and wait briefly for a reply.
        lock (_latestLock)
        {
            _latest = null;
        }

        await _transport.WriteAsync(_options.Encoding.GetBytes(_options.WeightCommand), cancellationToken);

        var arrived = await WaitForFreshReadingAsync(_options.AutoDetectTimeout, cancellationToken);
        _commandMode = arrived;
        _logger.LogInformation(
            "SerialScale '{DeviceId}' auto-detected {Mode} mode",
            Descriptor.Id,
            _commandMode ? "command" : "continuous");
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
    public async Task<WeightReading> GetWeightAsync(CancellationToken cancellationToken = default)
    {
        if (DeviceStatus != DeviceState.Ready)
        {
            throw new DeviceException(
                $"Scale '{Descriptor.Id}' is not ready. Current state: {DeviceStatus}.",
                Descriptor.Id,
                Descriptor.Type,
                ErrorCode.ConnectionFailed);
        }

        DeviceStatus = DeviceState.Busy;
        try
        {
            if (_commandMode)
            {
                lock (_latestLock)
                {
                    _latest = null;
                }

                await _transport.WriteAsync(_options.Encoding.GetBytes(_options.WeightCommand), cancellationToken);
                await WaitForFreshReadingAsync(_options.AutoDetectTimeout, cancellationToken);
            }

            WeightReading? latest;
            decimal tareOffset;
            lock (_latestLock)
            {
                latest = _latest;
                tareOffset = _tareOffset;
            }

            if (latest is null)
            {
                throw new DeviceException(
                    $"Scale '{Descriptor.Id}' produced no weight reading.",
                    Descriptor.Id,
                    Descriptor.Type,
                    ErrorCode.HardwareFailure);
            }

            var tared = latest with { Value = latest.Value - tareOffset, Timestamp = DateTimeOffset.UtcNow };
            DeviceStatus = DeviceState.Ready;
            return tared;
        }
        catch
        {
            DeviceStatus = DeviceState.Ready;
            throw;
        }
    }

    /// <inheritdoc />
    public Task TareAsync(CancellationToken cancellationToken = default)
    {
        // Software tare: there is no portable cross-vendor serial tare command in scope, so we capture
        // the current reading as the zero offset (mirroring MockScale's offset behavior). Subsequent
        // GetWeightAsync results subtract this offset.
        lock (_latestLock)
        {
            _tareOffset = _latest?.Value ?? 0m;
        }

        return Task.CompletedTask;
    }

    private async Task<bool> WaitForFreshReadingAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            lock (_latestLock)
            {
                if (_latest is not null) return true;
            }

            await Task.Delay(20, cancellationToken);
        }

        lock (_latestLock)
        {
            return _latest is not null;
        }
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
                    if (b == (byte)'\r' || b == (byte)'\n')
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
            _logger.LogWarning(ex, "SerialScale read loop terminated with exception");
        }
    }

    private void FlushLine()
    {
        if (_accumulator.Count == 0)
        {
            return;
        }

        var line = _options.Encoding.GetString(_accumulator.ToArray());
        _accumulator.Clear();

        var reading = ParseWeight(line);
        if (reading is not null)
        {
            lock (_latestLock)
            {
                _latest = reading;
            }
        }
    }

    private WeightReading? ParseWeight(string line)
    {
        var match = _weightRegex.Match(line);
        if (!match.Success)
        {
            return null;
        }

        var unit = match.Groups.Count > 1 && match.Groups[1].Success
            ? match.Groups[1].Value
            : _options.DefaultUnit;

        // Strip the unit suffix from the matched text to leave the numeric portion.
        var numeric = match.Value;
        if (match.Groups.Count > 1 && match.Groups[1].Success)
        {
            var unitIndex = numeric.LastIndexOf(match.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
            if (unitIndex >= 0)
            {
                numeric = numeric[..unitIndex];
            }
        }

        if (!decimal.TryParse(numeric.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        return new WeightReading(value, unit, IsStable: true, DateTimeOffset.UtcNow);
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
