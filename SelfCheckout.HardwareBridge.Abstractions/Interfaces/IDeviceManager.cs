using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface IDeviceManager : IDisposable
{

    event EventHandler<IDevice>? DeviceConnected;

    event EventHandler<IDevice>? DeviceDisconnected;

    void RegisterDevice(IDevice device);

    bool UnregisterDevice(string deviceId);

    IReadOnlyCollection<IDevice> GetDevices();

    T? GetDevice<T>() where T : class, IDevice;

    IDevice? GetDevice(string deviceId);
}
