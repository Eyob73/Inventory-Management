using Inventory_Management.Application.DTOs.Report;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;

namespace Inventory_Management.Application.Services;

public class InventoryReportDocument : IDocument
{
    private readonly string _tenantName;
    private readonly string _userName;
    private readonly DateTime _start;
    private readonly DateTime _end;

    private readonly DashboardReportDto _dashboard;
    private readonly SalesReportDto _sales;
    private readonly PurchasesReportDto _purchases;
    private readonly InventoryReportDto _inventory;

    // ============================================================
    // PROFESSIONAL REPORT COLORS
    // ============================================================

    private const string Primary = "#0F766E";
    private const string PrimaryDark = "#115E59";

    private const string Text = "#1E293B";
    private const string MutedText = "#64748B";

    private const string LightBackground = "#F8FAFC";
    private const string Border = "#E2E8F0";
    private const string HeaderBackground = "#F1F5F9";

    private const string Success = "#15803D";
    private const string Warning = "#D97706";
    private const string Danger = "#DC2626";

    public InventoryReportDocument(
        string tenantName,
        string userName,
        DateTime start,
        DateTime end,
        DashboardReportDto dashboard,
        SalesReportDto sales,
        PurchasesReportDto purchases,
        InventoryReportDto inventory)
    {
        _tenantName = tenantName;
        _userName = userName;
        _start = start;
        _end = end;

        _dashboard = dashboard;
        _sales = sales;
        _purchases = purchases;
        _inventory = inventory;
    }

    public DocumentMetadata GetMetadata()
    {
        return new DocumentMetadata
        {
            Title = "Inventory Management Report",
            Author = _tenantName,
            Subject = $"Inventory Report {_start:dd MMM yyyy} - {_end:dd MMM yyyy}",
            Creator = "Inventory Management System",
            Keywords = "Inventory, Sales, Purchases, Report"
        };
    }

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    // ============================================================
    // MAIN DOCUMENT
    // ============================================================

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.4f, Unit.Centimetre);

                page.PageColor(Colors.White);

                page.DefaultTextStyle(style =>
                    style
                        .FontFamily(Fonts.Arial)
                        .FontSize(9)
                        .FontColor(Text));

                page.Header().Element(ComposeHeader);

                page.Content()
                    .PaddingTop(8)
                    .Element(ComposeContent);

                page.Footer().Element(ComposeFooter);
            });
    }

    // ============================================================
    // HEADER
    // ============================================================

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item()
                        .Text(string.IsNullOrWhiteSpace(_tenantName)
                            ? "INVENTORY MANAGEMENT"
                            : _tenantName.ToUpper())
                        .FontSize(11)
                        .Bold()
                        .FontColor(PrimaryDark);

                    left.Item()
                        .PaddingTop(2)
                        .Text("Inventory Management Report")
                        .FontSize(8)
                        .FontColor(MutedText);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item()
                        .Text($"{_start:dd MMM yyyy} - {_end:dd MMM yyyy}")
                        .FontSize(8)
                        .FontColor(MutedText)
                        .AlignRight();

                    right.Item()
                        .PaddingTop(2)
                        .Text("CONFIDENTIAL")
                        .FontSize(7)
                        .Bold()
                        .FontColor(MutedText)
                        .AlignRight();
                });
            });

            column.Item()
                .PaddingTop(8)
                .LineHorizontal(1)
                .LineColor(Border);
        });
    }

    // ============================================================
    // CONTENT
    // ============================================================

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            ComposeCover(column);

            ComposeExecutiveSummary(column);

            ComposeSalesOverview(column);
            ComposeSalesDetails(column);

            ComposePurchaseOverview(column);
            ComposePurchaseDetails(column);

            ComposeInventorySummary(column);

            ComposeStockAlerts(column);

            ComposeProductPerformance(column);

            ComposeCategoryPerformance(column);

            ComposeSupplierPerformance(column);

            ComposeFinancialSummary(column);
        });
    }

    // ============================================================
    // COVER
    // ============================================================

    private void ComposeCover(ColumnDescriptor column)
    {
        column.Item()
            .PaddingBottom(25)
            .Border(1)
            .BorderColor(Border)
            .Background(LightBackground)
            .Padding(35)
            .Column(cover =>
            {
                cover.Item()
                    .AlignCenter()
                    .Text("INVENTORY MANAGEMENT")
                    .FontSize(10)
                    .Bold()
                    .FontColor(Primary);

                cover.Item()
                    .PaddingTop(12)
                    .AlignCenter()
                    .Text("REPORT")
                    .FontSize(26)
                    .Bold()
                    .FontColor(Text);

                cover.Item()
                    .PaddingTop(8)
                    .AlignCenter()
                    .Text(string.IsNullOrWhiteSpace(_tenantName)
                        ? "Company"
                        : _tenantName)
                    .FontSize(14)
                    .SemiBold()
                    .FontColor(MutedText);

                cover.Item()
                    .PaddingVertical(25)
                    .LineHorizontal(2)
                    .LineColor(Primary);

                cover.Item()
                    .AlignCenter()
                    .Text("REPORTING PERIOD")
                    .FontSize(8)
                    .Bold()
                    .FontColor(MutedText);

                cover.Item()
                    .PaddingTop(5)
                    .AlignCenter()
                    .Text($"{_start:dd MMMM yyyy} - {_end:dd MMMM yyyy}")
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(Text);

                cover.Item()
                    .PaddingTop(25)
                    .Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item()
                                .Text("GENERATED BY")
                                .FontSize(7)
                                .Bold()
                                .FontColor(MutedText);

                            c.Item()
                                .PaddingTop(3)
                                .Text(string.IsNullOrWhiteSpace(_userName)
                                    ? "System"
                                    : _userName)
                                .FontSize(9)
                                .SemiBold();
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item()
                                .Text("GENERATED")
                                .FontSize(7)
                                .Bold()
                                .FontColor(MutedText);

                            c.Item()
                                .PaddingTop(3)
                                .Text($"{DateTime.Now:dd MMMM yyyy, HH:mm}")
                                .FontSize(9)
                                .SemiBold();
                        });
                    });
            });
    }

    // ============================================================
    // EXECUTIVE SUMMARY
    // ============================================================

    private void ComposeExecutiveSummary(ColumnDescriptor column)
    {
        SectionTitle(
            column,
            "EXECUTIVE SUMMARY",
            "Overview of business and inventory performance for the selected period.");

        column.Item()
            .PaddingTop(10)
            .Row(row =>
            {
                row.Spacing(10);

                KpiCard(
                    row.RelativeItem(),
                    "TOTAL SALES",
                    $"{_sales.Summary.NumberOfSales:N0}");

                KpiCard(
                    row.RelativeItem(),
                    "TOTAL REVENUE",
                    FormatCurrency(_dashboard.TotalSales.Value));

                KpiCard(
                    row.RelativeItem(),
                    "GROSS PROFIT",
                    FormatCurrency(_dashboard.TotalProfit.Value));
            });

        column.Item()
            .PaddingTop(10)
            .PaddingBottom(25)
            .Row(row =>
            {
                row.Spacing(10);

                KpiCard(
                    row.RelativeItem(),
                    "TOTAL PURCHASES",
                    FormatCurrency(_dashboard.TotalPurchases.Value));

                KpiCard(
                    row.RelativeItem(),
                    "INVENTORY VALUE",
                    FormatCurrency(_inventory.Summary.InventoryValue));

                KpiCard(
                    row.RelativeItem(),
                    "LOW STOCK",
                    $"{_dashboard.LowStockProducts:N0}");
            });
    }

    private void KpiCard(
        IContainer container,
        string title,
        string value)
    {
        container
            .Border(1)
            .BorderColor(Border)
            .Background(Colors.White)
            .Padding(12)
            .Column(column =>
            {
                column.Item()
                    .Text(title)
                    .FontSize(7)
                    .Bold()
                    .FontColor(MutedText);

                column.Item()
                    .PaddingTop(6)
                    .Text(value)
                    .FontSize(12)
                    .Bold()
                    .FontColor(PrimaryDark);

                column.Item()
                    .PaddingTop(8)
                    .Height(2)
                    .Background(Primary);
            });
    }

    // ============================================================
    // SECTION TITLE
    // ============================================================

    private void SectionTitle(
        ColumnDescriptor column,
        string title,
        string? subtitle = null)
    {
        column.Item()
            .PaddingTop(12)
            .PaddingBottom(10)
            .Column(section =>
            {
                section.Item()
                    .Text(title)
                    .FontSize(13)
                    .Bold()
                    .FontColor(Text);

                if (!string.IsNullOrWhiteSpace(subtitle))
                {
                    section.Item()
                        .PaddingTop(3)
                        .Text(subtitle)
                        .FontSize(8)
                        .FontColor(MutedText);
                }

                section.Item()
                    .PaddingTop(7)
                    .LineHorizontal(1)
                    .LineColor(Border);
            });
    }

    // ============================================================
    // SALES OVERVIEW
    // ============================================================

    private void ComposeSalesOverview(ColumnDescriptor column)
    {
        SectionTitle(
            column,
            "SALES OVERVIEW",
            "Sales performance during the selected reporting period.");

        var revenue = _dashboard.TotalSales.Value;
        var profit = _dashboard.TotalProfit.Value;

        var cost = revenue - profit;

        var margin = revenue > 0
            ? profit / revenue * 100
            : 0;

        column.Item()
            .PaddingBottom(20)
            .Border(1)
            .BorderColor(Border)
            .Padding(14)
            .Column(summary =>
            {
                KeyValueRow(
                    summary,
                    "Transactions",
                    $"{_sales.Summary.NumberOfSales:N0}");

                KeyValueRow(
                    summary,
                    "Revenue",
                    FormatCurrency(revenue));

                KeyValueRow(
                    summary,
                    "Cost",
                    FormatCurrency(cost));

                KeyValueRow(
                    summary,
                    "Gross Profit",
                    FormatCurrency(profit));

                KeyValueRow(
                    summary,
                    "Gross Profit Margin",
                    $"{margin:N2}%");

                KeyValueRow(
                    summary,
                    "Average Sale",
                    FormatCurrency(_sales.Summary.AverageSaleValue));
            });
    }

    private void KeyValueRow(
        ColumnDescriptor column,
        string key,
        string value)
    {
        column.Item()
            .PaddingVertical(5)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text(key)
                    .FontSize(9)
                    .FontColor(MutedText);

                row.ConstantItem(170)
                    .AlignRight()
                    .Text(value)
                    .FontSize(9)
                    .SemiBold()
                    .FontColor(Text);
            });
    }

    // ============================================================
    // SALES DETAILS
    // ============================================================

    private void ComposeSalesDetails(ColumnDescriptor column)
    {
        if (!_sales.Table.Items.Any())
            return;

        SectionTitle(
            column,
            "SALES DETAILS",
            "All sales transactions during the selected reporting period.");

        column.Item()
            .PaddingBottom(20)
            .Element(container =>
                ComposeSalesTable(container));
    }

    private void ComposeSalesTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(2.3f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(1.5f);
            });

            table.Header(header =>
            {
                TableHeaderCell(header.Cell(), "DATE");
                TableHeaderCell(header.Cell(), "INVOICE");
                TableHeaderCell(header.Cell(), "CUSTOMER");
                TableHeaderCell(header.Cell(), "ITEMS");
                TableHeaderCell(header.Cell(), "TOTAL");
            });

            foreach (var item in _sales.Table.Items)
            {
                TableBodyCell(
                    table.Cell(),
                    item.Date.ToString("dd MMM yyyy"));

                TableBodyCell(
                    table.Cell(),
                    item.InvoiceNumber);

                TableBodyCell(
                    table.Cell(),
                    string.IsNullOrWhiteSpace(item.Customer)
                        ? "Walk-in"
                        : item.Customer);

                TableBodyCell(
                    table.Cell(),
                    item.NumberOfItems.ToString("N0"),
                    true);

                TableBodyCell(
                    table.Cell(),
                    FormatCurrency(item.TotalAmount),
                    true);
            }
        });
    }

    // ============================================================
    // PURCHASE OVERVIEW
    // ============================================================

    private void ComposePurchaseOverview(ColumnDescriptor column)
    {
        SectionTitle(
            column,
            "PURCHASE OVERVIEW",
            "Purchasing activity during the selected reporting period.");

        column.Item()
            .PaddingBottom(20)
            .Row(row =>
            {
                row.Spacing(10);

                KpiCard(
                    row.RelativeItem(),
                    "PURCHASE TRANSACTIONS",
                    $"{_purchases.Table.Items.Count:N0}");

                KpiCard(
                    row.RelativeItem(),
                    "TOTAL PURCHASE VALUE",
                    FormatCurrency(_dashboard.TotalPurchases.Value));
            });
    }

    // ============================================================
    // PURCHASE DETAILS
    // ============================================================

    private void ComposePurchaseDetails(ColumnDescriptor column)
    {
        if (!_purchases.Table.Items.Any())
            return;

        SectionTitle(
            column,
            "PURCHASE DETAILS",
            "All purchase transactions during the selected reporting period.");

        column.Item()
            .PaddingBottom(20)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(2.3f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    TableHeaderCell(header.Cell(), "DATE");
                    TableHeaderCell(header.Cell(), "REFERENCE");
                    TableHeaderCell(header.Cell(), "SUPPLIER");
                    TableHeaderCell(header.Cell(), "ITEMS");
                    TableHeaderCell(header.Cell(), "TOTAL");
                });

                foreach (var item in _purchases.Table.Items)
                {
                    TableBodyCell(
                        table.Cell(),
                        item.PurchaseDate.ToString("dd MMM yyyy"));

                    TableBodyCell(
                        table.Cell(),
                        item.PurchaseNumber);

                    TableBodyCell(
                        table.Cell(),
                        string.IsNullOrWhiteSpace(item.Supplier)
                            ? "-"
                            : item.Supplier);

                    TableBodyCell(
                        table.Cell(),
                        item.NumberOfItems.ToString("N0"),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        FormatCurrency(item.TotalAmount),
                        true);
                }
            });
    }

    // ============================================================
    // INVENTORY
    // ============================================================

    private void ComposeInventorySummary(ColumnDescriptor column)
    {
        SectionTitle(
            column,
            "CURRENT INVENTORY",
            "Current inventory position and product stock levels.");

        column.Item()
            .PaddingBottom(15)
            .Row(row =>
            {
                row.Spacing(10);

                KpiCard(
                    row.RelativeItem(),
                    "PRODUCTS",
                    $"{_inventory.Table.Items.Count:N0}");

                KpiCard(
                    row.RelativeItem(),
                    "INVENTORY VALUE",
                    FormatCurrency(_inventory.Summary.InventoryValue));

                var quantity = _inventory.Table.Items
                    .Sum(x => x.CurrentStock);

                KpiCard(
                    row.RelativeItem(),
                    "TOTAL QUANTITY",
                    $"{quantity:N0}");
            });

        if (!_inventory.Table.Items.Any())
            return;

        column.Item()
            .PaddingBottom(25)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(2.3f);
                    columns.RelativeColumn(1.8f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.3f);
                });

                table.Header(header =>
                {
                    TableHeaderCell(header.Cell(), "SKU");
                    TableHeaderCell(header.Cell(), "PRODUCT");
                    TableHeaderCell(header.Cell(), "CATEGORY");
                    TableHeaderCell(header.Cell(), "QTY");
                    TableHeaderCell(header.Cell(), "COST");
                    TableHeaderCell(header.Cell(), "SELLING PRICE");
                    TableHeaderCell(header.Cell(), "STATUS");
                });

                foreach (var item in _inventory.Table.Items)
                {
                    TableBodyCell(table.Cell(), item.SKU);
                    TableBodyCell(table.Cell(), item.Product);
                    TableBodyCell(
                        table.Cell(),
                        string.IsNullOrWhiteSpace(item.Category)
                            ? "-"
                            : item.Category);

                    TableBodyCell(
                        table.Cell(),
                        item.CurrentStock.ToString("N0"),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        FormatCurrency(item.UnitCost),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        FormatCurrency(item.SellingPrice),
                        true);

                    StockStatusCell(
                        table.Cell(),
                        item.StockStatus);
                }
            });
    }

    // ============================================================
    // STOCK ALERTS
    // ============================================================

    private void ComposeStockAlerts(ColumnDescriptor column)
    {
        var lowStock = _inventory.Table.Items
            .Where(x =>
                string.Equals(
                    x.StockStatus,
                    "Low Stock",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var outOfStock = _inventory.Table.Items
            .Where(x =>
                string.Equals(
                    x.StockStatus,
                    "Out of Stock",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!lowStock.Any() && !outOfStock.Any())
            return;

        SectionTitle(
            column,
            "STOCK ALERTS",
            "Products requiring inventory attention.");

        if (lowStock.Any())
        {
            column.Item()
                .PaddingBottom(8)
                .Text("LOW STOCK")
                .FontSize(10)
                .Bold()
                .FontColor(Warning);

            ComposeStockAlertTable(column, lowStock, "LOW");
        }

        if (outOfStock.Any())
        {
            column.Item()
                .PaddingTop(15)
                .PaddingBottom(8)
                .Text("OUT OF STOCK")
                .FontSize(10)
                .Bold()
                .FontColor(Danger);

            ComposeStockAlertTable(column, outOfStock, "OUT");
        }
    }

    private void ComposeStockAlertTable(
        ColumnDescriptor column,
        System.Collections.Generic.List<InventoryReportRowDto> items,
        string status)
    {
        column.Item()
            .PaddingBottom(15)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.8f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    TableHeaderCell(header.Cell(), "PRODUCT");
                    TableHeaderCell(header.Cell(), "SKU");
                    TableHeaderCell(header.Cell(), "CURRENT");
                    TableHeaderCell(header.Cell(), "MINIMUM");
                    TableHeaderCell(header.Cell(), "STATUS");
                });

                foreach (var item in items)
                {
                    TableBodyCell(table.Cell(), item.Product);
                    TableBodyCell(table.Cell(), item.SKU);

                    TableBodyCell(
                        table.Cell(),
                        item.CurrentStock.ToString("N0"),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        item.MinimumStock.ToString("N0"),
                        true);

                    table.Cell()
                        .Padding(6)
                        .AlignCenter()
                        .Text(status)
                        .FontSize(7)
                        .Bold()
                        .FontColor(
                            status == "LOW"
                                ? Warning
                                : Danger);
                }
            });
    }

    // ============================================================
    // PRODUCT PERFORMANCE
    // ============================================================

    private void ComposeProductPerformance(ColumnDescriptor column)
    {
        if (!_sales.TopSellingProducts.Any())
            return;

        SectionTitle(
            column,
            "PRODUCT PERFORMANCE",
            "Top-selling products during the selected reporting period.");

        column.Item()
            .PaddingBottom(20)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(45);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.8f);
                });

                table.Header(header =>
                {
                    TableHeaderCell(header.Cell(), "RANK");
                    TableHeaderCell(header.Cell(), "PRODUCT");
                    TableHeaderCell(header.Cell(), "QTY SOLD");
                    TableHeaderCell(header.Cell(), "REVENUE");
                });

                int rank = 1;

                foreach (var item in _sales.TopSellingProducts.Take(10))
                {
                    TableBodyCell(
                        table.Cell(),
                        rank.ToString(),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        item.Name);

                    TableBodyCell(
                        table.Cell(),
                        item.Quantity.ToString("N0"),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        FormatCurrency(item.Amount),
                        true);

                    rank++;
                }
            });
    }

    // ============================================================
    // CATEGORY PERFORMANCE
    // ============================================================

    private void ComposeCategoryPerformance(ColumnDescriptor column)
    {
        if (!_sales.SalesByCategory.Any())
            return;

        SectionTitle(
            column,
            "CATEGORY PERFORMANCE",
            "Sales performance grouped by product category.");

        column.Item()
            .PaddingBottom(20)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.8f);
                });

                table.Header(header =>
                {
                    TableHeaderCell(header.Cell(), "CATEGORY");
                    TableHeaderCell(header.Cell(), "QTY SOLD");
                    TableHeaderCell(header.Cell(), "REVENUE");
                });

                foreach (var item in _sales.SalesByCategory)
                {
                    TableBodyCell(table.Cell(), item.Name);

                    TableBodyCell(
                        table.Cell(),
                        item.Quantity.ToString("N0"),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        FormatCurrency(item.Amount),
                        true);
                }
            });
    }

    // ============================================================
    // SUPPLIER PERFORMANCE
    // ============================================================

    private void ComposeSupplierPerformance(ColumnDescriptor column)
    {
        if (!_purchases.PurchasesBySupplier.Any())
            return;

        SectionTitle(
            column,
            "SUPPLIER PERFORMANCE",
            "Purchasing activity grouped by supplier.");

        column.Item()
            .PaddingBottom(20)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.8f);
                });

                table.Header(header =>
                {
                    TableHeaderCell(header.Cell(), "SUPPLIER");
                    TableHeaderCell(header.Cell(), "QUANTITY");
                    TableHeaderCell(header.Cell(), "AMOUNT");
                });

                foreach (var item in _purchases.PurchasesBySupplier)
                {
                    TableBodyCell(table.Cell(), item.Name);

                    TableBodyCell(
                        table.Cell(),
                        item.Quantity.ToString("N0"),
                        true);

                    TableBodyCell(
                        table.Cell(),
                        FormatCurrency(item.Amount),
                        true);
                }
            });
    }

    // ============================================================
    // FINANCIAL SUMMARY
    // ============================================================

    private void ComposeFinancialSummary(ColumnDescriptor column)
    {
        column.Item()
            .PageBreak();

        SectionTitle(
            column,
            "FINANCIAL SUMMARY",
            "Financial performance for the selected reporting period.");

        var revenue = _dashboard.TotalSales.Value;
        var profit = _dashboard.TotalProfit.Value;

        var cost = revenue - profit;

        var margin = revenue > 0
            ? profit / revenue * 100
            : 0;

        column.Item()
            .Border(1)
            .BorderColor(Border)
            .Padding(15)
            .Column(summary =>
            {
                KeyValueRow(
                    summary,
                    "Total Revenue",
                    FormatCurrency(revenue));

                KeyValueRow(
                    summary,
                    "Cost of Goods Sold",
                    FormatCurrency(cost));

                summary.Item()
                    .PaddingVertical(10)
                    .LineHorizontal(1)
                    .LineColor(Border);

                summary.Item()
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text("Gross Profit")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Text);

                        row.ConstantItem(170)
                            .AlignRight()
                            .Text(FormatCurrency(profit))
                            .FontSize(12)
                            .Bold()
                            .FontColor(Success);
                    });

                KeyValueRow(
                    summary,
                    "Gross Profit Margin",
                    $"{margin:N2}%");
            });

        // --------------------------------------------------------
        // INVENTORY VALUATION
        // --------------------------------------------------------

        SectionTitle(
            column,
            "CURRENT INVENTORY VALUATION",
            "Estimated value of current inventory at cost and selling price.");

        decimal costValue = _inventory.Summary.InventoryValue;

        decimal sellingValue = _inventory.Table.Items
            .Sum(x => x.SellingPrice * x.CurrentStock);

        decimal potentialProfit = sellingValue - costValue;

        column.Item()
            .Border(1)
            .BorderColor(Border)
            .Padding(15)
            .Column(summary =>
            {
                KeyValueRow(
                    summary,
                    "Inventory Cost Value",
                    FormatCurrency(costValue));

                KeyValueRow(
                    summary,
                    "Inventory Selling Value",
                    FormatCurrency(sellingValue));

                summary.Item()
                    .PaddingVertical(10)
                    .LineHorizontal(1)
                    .LineColor(Border);

                summary.Item()
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text("Potential Inventory Profit")
                            .FontSize(10)
                            .Bold();

                        row.ConstantItem(170)
                            .AlignRight()
                            .Text(FormatCurrency(potentialProfit))
                            .FontSize(11)
                            .Bold()
                            .FontColor(Success);
                    });
            });

        // --------------------------------------------------------
        // REPORT END
        // --------------------------------------------------------

        column.Item()
            .PaddingTop(30)
            .AlignCenter()
            .Text("END OF REPORT")
            .FontSize(8)
            .Bold()
            .FontColor(MutedText);
    }

    // ============================================================
    // TABLE STYLING
    // ============================================================

    private void TableHeaderCell(
        IContainer container,
        string text)
    {
        container
            .Background(PrimaryDark)
            .PaddingVertical(7)
            .PaddingHorizontal(6)
            .Text(text)
            .FontSize(7)
            .Bold()
            .FontColor(Colors.White);
    }

    private void TableBodyCell(
        IContainer container,
        string? text,
        bool alignRight = false)
    {
        var textDesc = container
            .BorderBottom(1)
            .BorderColor(Border)
            .Background(LightBackground)
            .PaddingVertical(6)
            .PaddingHorizontal(6)
            .AlignMiddle()
            .Text(text ?? "-")
            .FontSize(7.5f)
            .FontColor(Text);
            
        if (alignRight)
            textDesc.AlignRight();
    }

    private void StockStatusCell(
        IContainer container,
        string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();

        var color = normalized switch
        {
            "low stock" => Warning,
            "out of stock" => Danger,
            "in stock" => Success,
            _ => MutedText
        };

        container
            .Padding(5)
            .AlignCenter()
            .Text(status ?? "-")
            .FontSize(7)
            .Bold()
            .FontColor(color);
    }

    // ============================================================
    // FOOTER
    // ============================================================

    private void ComposeFooter(IContainer container)
    {
        container
            .PaddingTop(8)
            .Column(column =>
            {
                column.Item()
                    .LineHorizontal(1)
                    .LineColor(Border);

                column.Item()
                    .PaddingTop(6)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text(text =>
                            {
                                text.Span("Inventory Management System")
                                    .FontSize(7)
                                    .FontColor(MutedText);

                                text.Span("  •  Confidential Business Report")
                                    .FontSize(7)
                                    .FontColor(MutedText);
                            });

                        row.RelativeItem()
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Page ")
                                    .FontSize(7)
                                    .FontColor(MutedText);

                                text.CurrentPageNumber()
                                    .FontSize(7)
                                    .Bold();

                                text.Span(" of ")
                                    .FontSize(7)
                                    .FontColor(MutedText);

                                text.TotalPages()
                                    .FontSize(7)
                                    .Bold();
                            });
                    });
            });
    }

    // ============================================================
    // FORMATTING
    // ============================================================

    private static string FormatCurrency(decimal amount)
    {
        return $"ETB {amount:N2}";
    }
}

// ================================================================
// PDF GENERATOR
// ================================================================

public static class CompositePdfGenerator
{
    public static byte[] Generate(
        string tenantName,
        string userName,
        DateTime start,
        DateTime end,
        DashboardReportDto dashboard,
        SalesReportDto sales,
        PurchasesReportDto purchases,
        InventoryReportDto inventory)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = new InventoryReportDocument(
            tenantName,
            userName,
            start,
            end,
            dashboard,
            sales,
            purchases,
            inventory);

        return document.GeneratePdf();
    }
}