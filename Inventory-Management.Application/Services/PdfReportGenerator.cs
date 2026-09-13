using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Inventory_Management.Application.Services;

public static class PdfReportGenerator
{
    public static byte[] Generate(string title, List<string[]> meta, string[] headers, List<string[]> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Element(c => ComposeHeader(c, title, meta));
                page.Content().Element(c => ComposeContent(c, headers, rows));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string title, List<string[]> meta)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Inventory Management System")
                    .FontSize(18).SemiBold().FontColor("#0B1F2E");
                
                column.Item().PaddingBottom(5).Text(title)
                    .FontSize(24).Bold().FontColor("#0E8C7F");
                
                foreach (var item in meta)
                {
                    if (item.Length >= 2 && item[0] != "Report")
                    {
                        column.Item().Text($"{item[0]}: {item[1]}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    }
                }
            });
        });
    }

    private static void ComposeContent(IContainer container, string[] headers, List<string[]> rows)
    {
        container.PaddingVertical(1, Unit.Centimetre).Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    columns.RelativeColumn();
                }
            });

            // Header row
            table.Header(header =>
            {
                foreach (var col in headers)
                {
                    header.Cell().Background("#0B1F2E")
                        .Padding(5)
                        .Text(col).FontColor(Colors.White).Bold();
                }
            });

            // Data rows
            var isAlternate = false;
            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    table.Cell().Background(isAlternate ? "#F8FAFC" : Colors.White)
                        .BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                        .Padding(5)
                        .Text(cell).FontSize(9);
                }
                isAlternate = !isAlternate;
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
            x.Span($"  |  Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }
}
