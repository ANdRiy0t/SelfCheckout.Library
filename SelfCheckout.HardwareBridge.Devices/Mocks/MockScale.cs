using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Devices.Mocks;

public class MockScale : DeviceBase, IWeightScale
{
    private readonly MockScaleOptions _options;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private decimal _tareOffset;

    public MockScale(DeviceDescriptor descriptor, MockScaleOptions? options = null)
        : base(descriptor)
    {
        _options = options ?? new MockScaleOptions();
    }

    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_options.ConnectDelay, cancellationToken);
    }

    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<WeightReading> GetWeightAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
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

            if (_options.ShouldFailOnRead)
            {
                DeviceStatus = DeviceState.Error;
                throw new DeviceException(
                    $"Scale '{Descriptor.Id}' failed during weight read.",
                    Descriptor.Id,
                    Descriptor.Type,
                    _options.FailureErrorCode);
            }

            await Task.Delay(_options.ReadDelay, cancellationToken);

            var result = new WeightReading(
                _options.DefaultWeight - _tareOffset,
                _options.Unit,
                _options.IsStable,
                DateTimeOffset.UtcNow);

            DeviceStatus = DeviceState.Ready;
            return result;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task TareAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            _tareOffset = _options.DefaultWeight;
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
