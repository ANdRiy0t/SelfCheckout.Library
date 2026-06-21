using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface IDevice
{

    DeviceDescriptor Descriptor { get; }

    DeviceState DeviceStatus { get; }

    event EventHandler<DeviceState>? StateChanged;

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
