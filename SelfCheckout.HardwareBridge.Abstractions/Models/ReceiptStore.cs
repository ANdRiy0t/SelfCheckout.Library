namespace SelfCheckout.HardwareBridge.Abstractions.Models;

/// <summary>Store header printed at the top of a receipt.</summary>
public record ReceiptStore(string Name, string Address);
