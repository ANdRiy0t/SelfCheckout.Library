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
/// Real <see cref="IPrinter"/> for ESC/POS receipt printers over a serial/USB-Serial port using
/// <see cref="SerialTransport"/>. Implements the minimal ESC/POS subset needed for a demo receipt:
/// initialize on connect (ESC @, <c>0x1B 0x40</c>), raw passthrough via <see cref="PrintRawAsync"/>,
/// and a full cut (GS V 0, <c>0x1D 0x56 0x00</c>) via <see cref="CutAsync"/>. This is not a full ESC/POS library.
/// </summary>
public class EscPosPrinter : DeviceBase, IPrinter
{
    private static readonly byte[] EscInit = [0x1B, 0x40];  // ESC @  — initialize printer
    private static readonly byte[] CutFull = [0x1D, 0x56, 0x00];  // GS V 0 — full cut

    private readonly SerialTransport _transport;
    private readonly SerialPrinterOptions _options;
    private readonly ILogger<EscPosPrinter> _logger;

    /// <summary>
    /// Initializes a new <see cref="EscPosPrinter"/>.
    /// </summary>
    /// <param name="descriptor">Identity and connection metadata for this device.</param>
    /// <param name="transportOptions">Serial-port configuration used to open the underlying transport.</param>
    /// <param name="options">Printer options (encoding, init-on-connect). Defaults applied when null.</param>
    /// <param name="logger">Optional logger. Defaults to <see cref="NullLogger{T}.Instance"/>.</param>
    public EscPosPrinter(
        DeviceDescriptor descriptor,
        SerialTransportOptions transportOptions,
        SerialPrinterOptions? options = null,
        ILogger<EscPosPrinter>? logger = null)
        : base(descriptor)
    {
        ArgumentNullException.ThrowIfNull(transportOptions);
        _transport = new SerialTransport(transportOptions, null);
        _options = options ?? new SerialPrinterOptions();
        _logger = logger ?? NullLogger<EscPosPrinter>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await _transport.OpenAsync(cancellationToken);
        if (_options.InitOnConnect)
        {
            await _transport.WriteAsync(EscInit, cancellationToken);
        }
    }

    /// <inheritdoc />
    protected override async Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        await _transport.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PrintRawAsync(byte[] data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (DeviceStatus != DeviceState.Ready)
        {
            throw new DeviceException(
                $"Printer '{Descriptor.Id}' is not ready. Current state: {DeviceStatus}.",
                Descriptor.Id,
                Descriptor.Type,
                ErrorCode.ConnectionFailed);
        }

        DeviceStatus = DeviceState.Busy;
        try
        {
            await _transport.WriteAsync(data, ct);
        }
        finally
        {
            DeviceStatus = DeviceState.Ready;
        }
    }

    /// <summary>
    /// Convenience helper (not part of <see cref="IPrinter"/>): encodes <paramref name="text"/> using the
    /// configured encoding and writes it via <see cref="PrintRawAsync"/>.
    /// </summary>
    /// <param name="text">The text to print.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public Task PrintTextAsync(string text, CancellationToken ct = default)
        => PrintRawAsync(_options.Encoding.GetBytes(text), ct);

    /// <inheritdoc />
    public async Task CutAsync(CancellationToken ct = default)
    {
        if (DeviceStatus != DeviceState.Ready)
        {
            throw new DeviceException(
                $"Printer '{Descriptor.Id}' is not ready. Current state: {DeviceStatus}.",
                Descriptor.Id,
                Descriptor.Type,
                ErrorCode.ConnectionFailed);
        }

        DeviceStatus = DeviceState.Busy;
        try
        {
            await _transport.WriteAsync(CutFull, ct);
        }
        finally
        {
            DeviceStatus = DeviceState.Ready;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }
}
