using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Devices.Mocks;

public class MockPaymentTerminal : DeviceBase, IPaymentTerminal
{
    private readonly MockPaymentTerminalOptions _options;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public MockPaymentTerminal(DeviceDescriptor descriptor, MockPaymentTerminalOptions? options = null)
        : base(descriptor)
    {
        _options = options ?? new MockPaymentTerminalOptions();
    }

    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_options.ConnectDelay, cancellationToken);
    }

    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            if (DeviceStatus != DeviceState.Ready)
            {
                throw new DeviceException(
                    $"Payment terminal '{Descriptor.Id}' is not ready. Current state: {DeviceStatus}.",
                    Descriptor.Id,
                    Descriptor.Type,
                    ErrorCode.ConnectionFailed);
            }

            DeviceStatus = DeviceState.Busy;

            if (_options.SimulateDisconnectOnProcess)
            {
                DeviceStatus = DeviceState.Disconnected;
                throw new DeviceException(
                    $"Payment terminal '{Descriptor.Id}' lost connection during payment processing.",
                    Descriptor.Id,
                    Descriptor.Type,
                    ErrorCode.ConnectionLost);
            }

            if (_options.ShouldFailOnProcess)
            {
                DeviceStatus = DeviceState.Error;
                throw new DeviceException(
                    $"Payment terminal '{Descriptor.Id}' failed during payment processing.",
                    Descriptor.Id,
                    Descriptor.Type,
                    _options.FailureErrorCode);
            }

            await Task.Delay(_options.ProcessDelay, cancellationToken);

            PaymentResult result;

            if (_options.ShouldDecline)
            {
                result = new PaymentResult(false, null, _options.DeclineReason, DateTimeOffset.UtcNow);
            }
            else
            {
                result = new PaymentResult(true, Guid.NewGuid().ToString(), null, DateTimeOffset.UtcNow);
            }

            DeviceStatus = DeviceState.Ready;
            return result;
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
