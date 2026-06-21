namespace SelfCheckout.HardwareBridge.Abstractions.Models;

/// <summary>A single line on a receipt.</summary>
public record ReceiptItem(string Name, decimal Quantity, decimal UnitPrice, string Unit = "pcs")
{
    /// <summary>Quantity multiplied by unit price.</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
