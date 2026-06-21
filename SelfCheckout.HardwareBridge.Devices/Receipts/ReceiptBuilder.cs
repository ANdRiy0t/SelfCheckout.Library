using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Devices.Receipts;

/// <summary>
/// Fluent builder that accumulates receipt data and produces an immutable <see cref="Receipt"/>.
/// All setters return <c>this</c> for chaining. <see cref="Build"/> validates the result.
/// </summary>
public sealed class ReceiptBuilder
{
    private readonly List<ReceiptItem> _items = new();
    private ReceiptStore _store = new(string.Empty, string.Empty);
    private DateTimeOffset _date = DateTimeOffset.UtcNow;
    private decimal _discount;
    private PaymentInfo _payment = new(PaymentMethod.Cash, 0m);
    private string _footer = string.Empty;

    /// <summary>Sets the store header.</summary>
    public ReceiptBuilder Store(string name, string address = "")
    {
        _store = new ReceiptStore(name, address);
        return this;
    }

    /// <summary>Sets the receipt date/time (defaults to UtcNow).</summary>
    public ReceiptBuilder Date(DateTimeOffset dt)
    {
        _date = dt;
        return this;
    }

    /// <summary>Appends a line item.</summary>
    public ReceiptBuilder AddItem(string name, decimal qty, decimal unitPrice, string unit = "pcs")
    {
        _items.Add(new ReceiptItem(name, qty, unitPrice, unit));
        return this;
    }

    /// <summary>Sets the discount applied to the subtotal.</summary>
    public ReceiptBuilder Discount(decimal amount)
    {
        _discount = amount;
        return this;
    }

    /// <summary>Sets the payment method and tendered amount.</summary>
    public ReceiptBuilder Payment(PaymentMethod method, decimal amount)
    {
        _payment = new PaymentInfo(method, amount);
        return this;
    }

    /// <summary>Sets the footer text.</summary>
    public ReceiptBuilder Footer(string text)
    {
        _footer = text;
        return this;
    }

    /// <summary>
    /// Builds the immutable <see cref="Receipt"/>.
    /// Throws <see cref="ArgumentException"/> if no items were added, and
    /// <see cref="ArgumentOutOfRangeException"/> if the payment amount is negative.
    /// </summary>
    public Receipt Build()
    {
        if (_items.Count == 0)
            throw new ArgumentException("Receipt must contain at least one item");

        if (_payment.Amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(_payment), "Payment amount must not be negative");

        return new Receipt(
            _store,
            _date,
            _items.ToList(),
            _discount,
            _payment,
            _footer);
    }
}
