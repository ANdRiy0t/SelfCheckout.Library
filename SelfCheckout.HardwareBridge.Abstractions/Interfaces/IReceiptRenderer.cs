using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

/// <summary>Renders a <see cref="Receipt"/> to a byte payload (PDF, ESC/POS, etc.).</summary>
public interface IReceiptRenderer
{
    /// <summary>Renders the receipt and returns the encoded bytes.</summary>
    Task<byte[]> RenderAsync(Receipt receipt, CancellationToken ct = default);
}
