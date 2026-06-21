using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Devices.Receipts;

/// <summary>
/// Renders a <see cref="Receipt"/> to an 80mm-wide PDF (226pt, continuous height) using QuestPDF.
/// Sets the QuestPDF Community license once in the constructor.
/// </summary>
public sealed class PdfReceiptRenderer : IReceiptRenderer
{
    private const float PageWidthPoints = 226f; // 80mm thermal roll

    /// <summary>Creates the renderer and registers the QuestPDF Community license.</summary>
    public PdfReceiptRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(Receipt receipt, CancellationToken ct = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.ContinuousSize(PageWidthPoints, Unit.Point);
                page.Margin(8, Unit.Point);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text(receipt.Store.Name)
                        .Bold().FontSize(14);

                    if (!string.IsNullOrWhiteSpace(receipt.Store.Address))
                    {
                        col.Item().AlignCenter().Text(receipt.Store.Address)
                            .FontColor(Colors.Grey.Medium).FontSize(9);
                    }

                    col.Item().AlignRight().Text(receipt.Date.LocalDateTime.ToString("g"))
                        .FontSize(9);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                    // Items table: Name (fill) | Qty x Price (right) | LineTotal (right)
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(80);
                            c.ConstantColumn(60);
                        });

                        foreach (var item in receipt.Items)
                        {
                            table.Cell().Text(item.Name).FontSize(10);
                            table.Cell().AlignRight().Text($"{item.Quantity:0.##}x{item.UnitPrice:0.00}").FontSize(10);
                            table.Cell().AlignRight().Text($"{item.LineTotal:0.00}").FontSize(10);
                        }
                    });

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                    col.Item().AlignRight().Text($"Subtotal: {receipt.Subtotal:C}").FontSize(10);

                    if (receipt.Discount > 0m)
                    {
                        col.Item().AlignRight().Text($"Discount: -{receipt.Discount:C}")
                            .FontColor(Colors.Red.Medium).FontSize(10);
                    }

                    col.Item().AlignRight().Text($"TOTAL: {receipt.Total:C}")
                        .Bold().FontSize(13);

                    col.Item().AlignRight().Text($"{receipt.Payment.Method} {receipt.Payment.Amount:C}")
                        .FontSize(9);

                    if (!string.IsNullOrWhiteSpace(receipt.Footer))
                    {
                        col.Item().PaddingTop(4).AlignCenter().Text(receipt.Footer)
                            .Italic().FontColor(Colors.Grey.Medium).FontSize(9);
                    }
                });
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
