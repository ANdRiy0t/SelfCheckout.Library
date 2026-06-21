using System.Text;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Devices.Receipts;

/// <summary>
/// Renders a <see cref="Receipt"/> to raw ESC/POS thermal printer bytes at a 42-character line
/// width. Pure byte assembly with no external dependencies. Emits its own cut (GS V 0) at the end.
/// </summary>
public sealed class EscPosReceiptRenderer : IReceiptRenderer
{
    private const int LineWidth = 42;

    // ESC/POS control sequences.
    private static readonly byte[] Init = { 0x1B, 0x40 };          // ESC @
    private static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };   // ESC a 0
    private static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 }; // ESC a 1
    private static readonly byte[] AlignRight = { 0x1B, 0x61, 0x02 };  // ESC a 2
    private static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };  // ESC E 1
    private static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 }; // ESC E 0
    private static readonly byte[] Cut = { 0x1D, 0x56, 0x00 };     // GS V 0

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(Receipt receipt, CancellationToken ct = default)
    {
        var buffer = new List<byte>();

        buffer.AddRange(Init);

        // Store header (centered, bold name).
        buffer.AddRange(AlignCenter);
        buffer.AddRange(BoldOn);
        AppendLine(buffer, receipt.Store.Name);
        buffer.AddRange(BoldOff);

        if (!string.IsNullOrWhiteSpace(receipt.Store.Address))
            AppendLine(buffer, receipt.Store.Address);

        // Date right-aligned.
        buffer.AddRange(AlignRight);
        AppendLine(buffer, receipt.Date.LocalDateTime.ToString("g"));

        // Body left-aligned.
        buffer.AddRange(AlignLeft);
        AppendLine(buffer, new string('-', LineWidth));

        foreach (var item in receipt.Items)
        {
            // First row: item name (truncated to fit).
            AppendLine(buffer, Truncate(item.Name, LineWidth));
            // Second row: qty x price on the left, line total on the right.
            var left = $"  {item.Quantity:0.##} x {item.UnitPrice:0.00}";
            AppendLine(buffer, PadLabelValue(left, item.LineTotal.ToString("0.00")));
        }

        AppendLine(buffer, new string('-', LineWidth));

        // Totals, right-aligned semantics via padded label+value rows.
        AppendLine(buffer, PadLabelValue("Subtotal", receipt.Subtotal.ToString("0.00")));

        if (receipt.Discount > 0m)
            AppendLine(buffer, PadLabelValue("Discount", "-" + receipt.Discount.ToString("0.00")));

        buffer.AddRange(BoldOn);
        AppendLine(buffer, PadLabelValue("TOTAL", receipt.Total.ToString("0.00")));
        buffer.AddRange(BoldOff);

        AppendLine(buffer, PadLabelValue(receipt.Payment.Method.ToString(), receipt.Payment.Amount.ToString("0.00")));

        // Footer centered.
        if (!string.IsNullOrWhiteSpace(receipt.Footer))
        {
            buffer.AddRange(AlignCenter);
            AppendLine(buffer, receipt.Footer);
            buffer.AddRange(AlignLeft);
        }

        // Feed and cut.
        AppendLine(buffer, string.Empty);
        AppendLine(buffer, string.Empty);
        buffer.AddRange(Cut);

        return Task.FromResult(buffer.ToArray());
    }

    /// <summary>Appends an ASCII string followed by a line feed.</summary>
    private static void AppendLine(List<byte> buffer, string text)
    {
        buffer.AddRange(Encoding.ASCII.GetBytes(text + "\n"));
    }

    /// <summary>Truncates a string to the given max length.</summary>
    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value.Substring(0, max);

    /// <summary>
    /// Composes a single 42-char line with <paramref name="label"/> on the left and
    /// <paramref name="value"/> right-aligned. Truncates the label if the combined length overflows.
    /// </summary>
    private static string PadLabelValue(string label, string value)
    {
        var space = LineWidth - value.Length;
        if (space < 1)
            return Truncate(value, LineWidth);

        if (label.Length > space - 1)
            label = Truncate(label, space - 1);

        return label.PadRight(space) + value;
    }
}
