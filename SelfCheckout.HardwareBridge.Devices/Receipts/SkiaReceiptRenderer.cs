using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SkiaSharp;

namespace SelfCheckout.HardwareBridge.Devices.Receipts;

/// <summary>
/// Renders a <see cref="Receipt"/> to an 80mm-wide PDF (226pt, estimated continuous height)
/// using SkiaSharp's PDF document API.
/// </summary>
/// <remarks>
/// Unlike <see cref="PdfReceiptRenderer"/> (QuestPDF), this renderer works on Android because
/// SkiaSharp ships native Skia binaries for Android (arm64/arm/x86), whereas QuestPDF's bundled
/// native Skia library is not compiled for Android and throws <c>TypeInitializationException</c>.
/// </remarks>
public sealed class SkiaReceiptRenderer : IReceiptRenderer
{
    private const float PageWidthPoints = 226f; // 80mm thermal roll
    private const float Margin = 8f;            // left/right margin

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(Receipt receipt, CancellationToken ct = default)
    {
        // Estimate page height from item count (name line + detail line per item).
        float height = Math.Max(200f, 80f + receipt.Items.Count * 25f + 80f);

        using var ms = new MemoryStream();
        using var document = SKDocument.CreatePdf(ms);

        // Canvas is owned by the document; EndPage handles its disposal.
        var canvas = document.BeginPage(PageWidthPoints, height);

        using var titleFont = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Bold), 16f);
        using var totalFont = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Bold), 13f);
        using var bodyFont = new SKFont(SKTypeface.Default, 10f);
        using var smallFont = new SKFont(SKTypeface.Default, 9f);

        using var blackPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var grayPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true };

        float y = 20f;

        // 1. Store name — centered, bold, 16f.
        DrawText(canvas, receipt.Store.Name, PageWidthPoints / 2f, y, titleFont, blackPaint, SKTextAlign.Center);
        y += 20f;

        // 2. Store address — centered, gray, 9f (if present).
        if (!string.IsNullOrWhiteSpace(receipt.Store.Address))
        {
            DrawText(canvas, receipt.Store.Address, PageWidthPoints / 2f, y, smallFont, grayPaint, SKTextAlign.Center);
            y += 13f;
        }

        // 3. Date — right-aligned, black, 9f.
        DrawText(canvas, receipt.Date.LocalDateTime.ToString("g"), PageWidthPoints - Margin, y, smallFont, blackPaint, SKTextAlign.Right);
        y += 10f;

        // 4. Separator.
        canvas.DrawLine(Margin, y, PageWidthPoints - Margin, y, grayPaint);
        y += 8f;

        // 5. Items.
        foreach (var item in receipt.Items)
        {
            DrawText(canvas, item.Name, Margin, y, bodyFont, blackPaint, SKTextAlign.Left);
            DrawText(canvas, item.LineTotal.ToString("0.00"), PageWidthPoints - Margin, y, bodyFont, blackPaint, SKTextAlign.Right);
            y += 12f;

            DrawText(canvas, $"  {item.Quantity:0.##} x {item.UnitPrice:0.00}", Margin, y, smallFont, grayPaint, SKTextAlign.Left);
            y += 13f;
        }

        // 6. Separator.
        canvas.DrawLine(Margin, y, PageWidthPoints - Margin, y, grayPaint);
        y += 8f;

        // 7. Subtotal — right-aligned, 10f.
        DrawText(canvas, $"Subtotal: {receipt.Subtotal:0.00}", PageWidthPoints - Margin, y, bodyFont, blackPaint, SKTextAlign.Right);
        y += 14f;

        // 8. Total — right-aligned, bold, 13f.
        DrawText(canvas, $"TOTAL: {receipt.Total:0.00}", PageWidthPoints - Margin, y, totalFont, blackPaint, SKTextAlign.Right);
        y += 18f;

        // 9. Payment — right-aligned, gray, 9f.
        DrawText(canvas, $"{receipt.Payment.Method} {receipt.Payment.Amount:0.00}", PageWidthPoints - Margin, y, smallFont, grayPaint, SKTextAlign.Right);
        y += 14f;

        // 10. Footer — separator + centered gray 9f (if present).
        if (!string.IsNullOrWhiteSpace(receipt.Footer))
        {
            canvas.DrawLine(Margin, y, PageWidthPoints - Margin, y, grayPaint);
            y += 8f;
            DrawText(canvas, receipt.Footer, PageWidthPoints / 2f, y, smallFont, grayPaint, SKTextAlign.Center);
            y += 14f;
        }

        document.EndPage();
        document.Close();

        return Task.FromResult(ms.ToArray());
    }

    /// <summary>
    /// Draws text using the SkiaSharp 2.88.x 5-argument <c>DrawText(string, x, y, SKFont, SKPaint)</c>
    /// overload, applying alignment via <see cref="SKPaint.TextAlign"/> (the 6-argument overload with
    /// an <see cref="SKTextAlign"/> parameter does not exist until SkiaSharp 3.116+).
    /// </summary>
    private static void DrawText(SKCanvas canvas, string text, float x, float y, SKFont font, SKPaint paint, SKTextAlign align)
    {
        paint.TextAlign = align;
        canvas.DrawText(text, x, y, font, paint);
    }
}
