namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface IPrinter : IDevice
{

    Task PrintRawAsync(byte[] data, CancellationToken ct = default);

    Task CutAsync(CancellationToken ct = default);
}
