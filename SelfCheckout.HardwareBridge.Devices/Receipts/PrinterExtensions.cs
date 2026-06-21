using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Devices.Receipts;

/// <summary>Receipt-printing extensions for <see cref="IPrinter"/>.</summary>
public static class PrinterExtensions
{
    /// <summary>
    /// Renders <paramref name="receipt"/> with <paramref name="renderer"/> and prints the resulting
    /// bytes via <see cref="IPrinter.PrintRawAsync"/>.
    /// <para>
    /// This does NOT call <see cref="IPrinter.CutAsync"/>: the ESC/POS renderer already emits a
    /// GS V 0 cut, and the PDF path has no cut concept. Callers that need a separate cut on the
    /// thermal hardware path may invoke <see cref="IPrinter.CutAsync"/> themselves.
    /// </para>
    /// </summary>
    public static async Task PrintReceiptAsync(
        this IPrinter printer,
        Receipt receipt,
        IReceiptRenderer renderer,
        CancellationToken ct = default)
    {
        var bytes = await renderer.RenderAsync(receipt, ct);
        await printer.PrintRawAsync(bytes, ct);
    }
}
