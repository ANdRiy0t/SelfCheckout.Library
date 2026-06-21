namespace SelfCheckout.HardwareBridge.Abstractions.Events;

public interface IDeviceEventHandler<TEvent>
{

    Task HandleAsync(TEvent ev, CancellationToken ct = default);
}
