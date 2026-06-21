using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface IWeightScale : IDevice
{

    Task<WeightReading> GetWeightAsync(CancellationToken cancellationToken = default);

    Task TareAsync(CancellationToken cancellationToken = default);
}
