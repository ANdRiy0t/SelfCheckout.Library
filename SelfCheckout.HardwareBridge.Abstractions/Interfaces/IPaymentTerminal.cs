using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface IPaymentTerminal : IDevice
{

    Task<PaymentResult> ProcessPaymentAsync(
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
}
