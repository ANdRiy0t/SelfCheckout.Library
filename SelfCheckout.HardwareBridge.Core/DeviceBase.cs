using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Core;

public abstract class DeviceBase : IDevice, IDisposable
{
    private DeviceState _status = DeviceState.Disconnected;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _disposed;

    public DeviceDescriptor Descriptor { get; }

    public DeviceState DeviceStatus
    {
        get => _status;
        protected set
        {
            if (_status == value) return;
            _status = value;
            StateChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<DeviceState>? StateChanged;

    protected DeviceBase(DeviceDescriptor descriptor)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (DeviceStatus == DeviceState.Ready) return;

            DeviceStatus = DeviceState.Connecting;
            try
            {
                await ConnectCoreAsync(cancellationToken);
                DeviceStatus = DeviceState.Ready;
            }
            catch (Exception ex) when (ex is not DeviceException)
            {
                DeviceStatus = DeviceState.Error;
                throw new DeviceException(
                    $"Failed to connect to device '{Descriptor.Id}'.",
                    Descriptor.Id,
                    Descriptor.Type,
                    ErrorCode.ConnectionFailed,
                    ex);
            }
            catch (DeviceException)
            {
                DeviceStatus = DeviceState.Error;
                throw;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (DeviceStatus == DeviceState.Disconnected) return;

            try
            {
                await DisconnectCoreAsync(cancellationToken);
            }
            finally
            {
                DeviceStatus = DeviceState.Disconnected;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    protected abstract Task ConnectCoreAsync(CancellationToken cancellationToken);

    protected abstract Task DisconnectCoreAsync(CancellationToken cancellationToken);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _connectionLock.Dispose();
        }
        _disposed = true;
    }
}
