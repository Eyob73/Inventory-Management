using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory_Management.Application.Services;

public static class PdfReportGenerator
{
    // ============================================================
    // PROFESSIONAL REPORT THEME
    // ============================================================

    private const string Primary = "#0F766E";
    private const string PrimaryDark = "#115E59";

    private const string Text = "#1E293B";
    private const string MutedText = "#64748B";

    private const string LightBackground = "#F8FAFC";
    private const string HeaderBackground = "#F1F5F9";
    private const string Border = "#E2E8F0";

    // ============================================================
    // GENERATE
    // ============================================================

    public static byte[] Generate(
        string title,
        List<string[]> meta,
        string[] headers,
        List<string[]> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var tenantName = GetMetaValue(
            meta,
            "Tenant",
            "Company Name");

        var userName = GetMetaValue(
            meta,
            "User",
            "Admin");

        var dateRange = GetMetaValue(
            meta,
            "Date Range",
            string.Empty);

        var generatedAt = GetMetaValue(
            meta,
            "Generated",
            DateTime.Now.ToString("dd MMM yyyy, HH:mm"));

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);

                page.MarginHorizontal(
                    1.4f,
                    Unit.Centimetre);

                page.MarginVertical(
                    1.35f,
                    Unit.Centimetre);

                page.PageColor(Colors.White);

                page.DefaultTextStyle(style =>
                    style
                        .FontFamily(Fonts.Arial)
                        .FontSize(9)
                        .FontColor(Text));

                page.Header()
                    .Element(container =>
                        ComposeHeader(
                            container,
                            title,
                            tenantName,
                            dateRange));

                page.Content()
                    .PaddingTop(10)
                    .Element(container =>
                        ComposeContent(
                            container,
                            headers,
                            rows));

                page.Footer()
                    .Element(footer =>
                        ComposeFooter(
                            footer,
                            tenantName,
                            userName,
                            generatedAt));
            });
        });

        return document.GeneratePdf();
    }

    // ============================================================
    // HEADER
    // ============================================================

    private static void ComposeHeader(
        IContainer container,
        string title,
        string tenantName,
        string dateRange)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item()
                        .Text(tenantName.ToUpperInvariant())
                        .FontSize(10.5f)
                        .Bold()
                        .FontColor(PrimaryDark);

                    left.Item()
                        .PaddingTop(2)
                        .Text("Inventory Management System")
                        .FontSize(7.5f)
                        .FontColor(MutedText);
                });

                row.RelativeItem()
                    .AlignRight()
                    .Column(right =>
                    {
                        right.Item()
                            .Text(title.ToUpperInvariant())
                            .FontSize(8)
                            .Bold()
                            .FontColor(PrimaryDark)
                            .AlignRight();

                        if (!string.IsNullOrWhiteSpace(dateRange))
                        {
                            right.Item()
                                .PaddingTop(2)
                                .Text(dateRange)
                                .FontSize(7.5f)
                                .FontColor(MutedText)
                                .AlignRight();
                        }
                    });
            });

            column.Item()
                .PaddingTop(7)
                .LineHorizontal(1)
                .LineColor(Border);
        });
    }

    // ============================================================
    // CONTENT
    // ============================================================

    private static void ComposeContent(
        IContainer container,
        string[] headers,
        List<string[]> rows)
    {
        container.Column(column =>
        {
            ComposeReportTitle(
                column,
                headers,
                rows);

            if (rows == null || rows.Count == 0)
            {
                ComposeEmptyState(column);
                return;
            }

            ComposeTable(
                column,
                headers,
                rows);
        });
    }

    // ============================================================
    // REPORT TITLE
    // ============================================================

    private static void ComposeReportTitle(
        ColumnDescriptor column,
        string[] headers,
        List<string[]> rows)
    {
        column.Item()
            .PaddingBottom(12)
            .Column(title =>
            {
                title.Item()
                    .Text("REPORT")
                    .FontSize(7)
                    .Bold()
                    .FontColor(Primary);

                title.Item()
                    .PaddingTop(3)
                    .Text("Detailed Data Report")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Text);

                title.Item()
                    .PaddingTop(3)
                    .Text(
                        $"{rows?.Count ?? 0:N0} record(s)")
                    .FontSize(7.5f)
                    .FontColor(MutedText);

                title.Item()
                    .PaddingTop(7)
                    .LineHorizontal(1)
                    .LineColor(Border);
            });
    }

    // ============================================================
    // TABLE
    // ============================================================

    private static void ComposeTable(
        ColumnDescriptor column,
        string[] headers,
        List<string[]> rows)
    {
        column.Item()
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in headers)
                    {
                        columns.RelativeColumn();
                    }
                });

                // ------------------------------------------------
                // HEADER
                // ------------------------------------------------

                table.Header(header =>
                {
                    foreach (var headerText in headers)
                    {
                        header.Cell()
                            .Background(PrimaryDark)
                            .PaddingVertical(7)
                            .PaddingHorizontal(6)
                            .Text(headerText.ToUpperInvariant())
                            .FontSize(6.8f)
                            .Bold()
                            .FontColor(Colors.White);
                    }
                });

                // ------------------------------------------------
                // BODY
                // ------------------------------------------------

                foreach (var row in rows)
                {
                    for (var i = 0; i < headers.Length; i++)
                    {
                        var value =
                            i < row.Length
                                ? row[i]
                                : "-";

                        table.Cell()
                            .BorderBottom(1)
                            .BorderColor(Border)
                            .Background(Colors.White)
                            .PaddingVertical(6)
                            .PaddingHorizontal(6)
                            .AlignMiddle()
                            .Text(value ?? "-")
                            .FontSize(7.2f)
                            .FontColor(Text);
                    }
                }
            });
    }

    // ============================================================
    // EMPTY STATE
    // ============================================================

    private static void ComposeEmptyState(
        ColumnDescriptor column)
    {
        column.Item()
            .Padding(25)
            .Border(1)
            .BorderColor(Border)
            .Background(LightBackground)
            .AlignCenter()
            .Column(empty =>
            {
                empty.Item()
                    .Text("NO DATA AVAILABLE")
                    .FontSize(10)
                    .Bold()
                    .FontColor(Text);

                empty.Item()
                    .PaddingTop(5)
                    .Text("There are no records to display for the selected criteria.")
                    .FontSize(8)
                    .FontColor(MutedText)
                    .AlignCenter();
            });
    }

    // ============================================================
    // FOOTER
    // ============================================================

    private static void ComposeFooter(
        IContainer container,
        string tenantName,
        string userName,
        string generatedAt)
    {
        container
            .PaddingTop(7)
            .Column(column =>
            {
                column.Item()
                    .LineHorizontal(1)
                    .LineColor(Border);

                column.Item()
                    .PaddingTop(5)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Column(left =>
                            {
                                left.Item()
                                    .Text(text =>
                                    {
                                        text.Span(tenantName)
                                            .FontSize(7)
                                            .Bold()
                                            .FontColor(MutedText);

                                        text.Span(
                                                "  •  Inventory Management System")
                                            .FontSize(7)
                                            .FontColor(MutedText);
                                    });

                                left.Item()
                                    .PaddingTop(2)
                                    .Text(
                                        $"Generated by {userName} • {generatedAt}")
                                    .FontSize(6.5f)
                                    .FontColor(MutedText);
                            });

                        row.ConstantItem(80)
                            .AlignRight()
                            .AlignBottom()
                            .Text(text =>
                            {
                                text.Span("Page ")
                                    .FontSize(7)
                                    .FontColor(MutedText);

                                text.CurrentPageNumber()
                                    .FontSize(7)
                                    .Bold()
                                    .FontColor(Text);

                                text.Span(" of ")
                                    .FontSize(7)
                                    .FontColor(MutedText);

                                text.TotalPages()
                                    .FontSize(7)
                                    .Bold()
                                    .FontColor(Text);
                            });
                    });
            });
    }

    // ============================================================
    // META HELPER
    // ============================================================

    private static string GetMetaValue(
        List<string[]> meta,
        string key,
        string fallback)
    {
        if (meta == null)
            return fallback;

        var value = meta
            .FirstOrDefault(x =>
                x != null &&
                x.Length >= 2 &&
                string.Equals(
                    x[0],
                    key,
                    StringComparison.OrdinalIgnoreCase));

        if (value == null)
            return fallback;

        return string.IsNullOrWhiteSpace(value[1])
            ? fallback
            : value[1];
    }
}