using System.Collections.Concurrent;
using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;

namespace SelfCheckout.HardwareBridge.Core;

public class DeviceManager : IDeviceManager
{
    private readonly ConcurrentDictionary<string, IDevice> _devices = new();
    private bool _disposed;

    public event EventHandler<IDevice>? DeviceConnected;

    public event EventHandler<IDevice>? DeviceDisconnected;

    public void RegisterDevice(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_devices.TryAdd(device.Descriptor.Id, device))
        {
            device.StateChanged += OnDeviceStateChanged;
            DeviceConnected?.Invoke(this, device);
        }
    }

    public bool UnregisterDevice(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        if (_devices.TryRemove(deviceId, out var device))
        {
            device.StateChanged -= OnDeviceStateChanged;
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<IDevice> GetDevices()
        => _devices.Values.ToList().AsReadOnly();

    public T? GetDevice<T>() where T : class, IDevice
        => _devices.Values.OfType<T>().FirstOrDefault();

    public IDevice? GetDevice(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        return _devices.TryGetValue(deviceId, out var device) ? device : null;
    }

    private void OnDeviceStateChanged(object? sender, DeviceState newState)
    {
        if (sender is IDevice device &&
            newState is DeviceState.Disconnected or DeviceState.Error)
        {
            if (_devices.TryRemove(device.Descriptor.Id, out _))
            {
                device.StateChanged -= OnDeviceStateChanged;
                DeviceDisconnected?.Invoke(this, device);
            }
        }
    }

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

            foreach (var device in _devices.Values)
            {
                device.StateChanged -= OnDeviceStateChanged;
            }
            _devices.Clear();
        }
        _disposed = true;
    }
}
