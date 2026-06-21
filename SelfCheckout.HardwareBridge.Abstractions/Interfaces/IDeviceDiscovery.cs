using SelfCheckout.HardwareBridge.Abstractions.Events;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface IDeviceDiscovery
{

    event EventHandler<DeviceChangeEvent>? Changed;

    Task StartAsync(CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);
}
