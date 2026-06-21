using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Devices.Mocks;

public class MockPrinter : DeviceBase, IPrinter
{
    private readonly MockPrinterOptions _options;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly List<byte[]> _printedPayloads = new();

    public MockPrinter(DeviceDescriptor descriptor, MockPrinterOptions? options = null)
        : base(descriptor)
    {
        _options = options ?? new MockPrinterOptions();
    }

    public IReadOnlyList<byte[]> PrintedPayloads
    {
        get
        {
            lock (_printedPayloads)
            {
                return _printedPayloads.ToArray();
            }
        }
    }

    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_options.ConnectDelay, cancellationToken);
    }

    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task PrintRawAsync(byte[] data, CancellationToken ct = default)
    {
        await _operationLock.WaitAsync(ct);
        try
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

            if (_options.SimulateOutOfPaper)
            {
                DeviceStatus = DeviceState.Error;
                throw new DeviceException(
                    $"Printer '{Descriptor.Id}' is out of paper.",
                    Descriptor.Id,
                    Descriptor.Type,
                    ErrorCode.HardwareFailure);
            }

            if (_options.ShouldFailOnPrint)
            {
                DeviceStatus = DeviceState.Error;
                throw new DeviceException(
                    $"Printer '{Descriptor.Id}' failed during print.",
                    Descriptor.Id,
                    Descriptor.Type,
                    _options.FailureErrorCode);
            }

            await Task.Delay(_options.PrintDelay, ct);

            var copy = (byte[])data.Clone();
            lock (_printedPayloads)
            {
                _printedPayloads.Add(copy);
            }

            DeviceStatus = DeviceState.Ready;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task CutAsync(CancellationToken ct = default)
    {
        await _operationLock.WaitAsync(ct);
        try
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

            if (_options.ShouldFailOnPrint)
            {
                DeviceStatus = DeviceState.Error;
                throw new DeviceException(
                    $"Printer '{Descriptor.Id}' failed during cut.",
                    Descriptor.Id,
                    Descriptor.Type,
                    _options.FailureErrorCode);
            }

            await Task.Delay(_options.PrintDelay, ct);

            DeviceStatus = DeviceState.Ready;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _operationLock.Dispose();
        }

        base.Dispose(disposing);
    }
}
