using System.Collections.Generic;
using System.Linq;

namespace SelfCheckout.HardwareBridge.Abstractions.Models;

/// <summary>Immutable receipt with computed <see cref="Subtotal"/> and <see cref="Total"/>.</summary>
public record Receipt(
    ReceiptStore Store,
    DateTimeOffset Date,
    IReadOnlyList<ReceiptItem> Items,
    decimal Discount,
    PaymentInfo Payment,
    string Footer)
{
    /// <summary>Sum of all item line totals before discount.</summary>
    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    /// <summary>Subtotal minus discount, clamped at zero.</summary>
    public decimal Total => Math.Max(0m, Subtotal - Discount);
}
