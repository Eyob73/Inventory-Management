using Inventory_Management.Application.DTOs.Common;

namespace Inventory_Management.Application.DTOs.Report;

public class SalesReportSummaryDto
{
    public decimal TotalSalesAmount { get; set; }
    public int NumberOfSales { get; set; }
    public int TotalProductsSold { get; set; }
    public decimal AverageSaleValue { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalTax { get; set; }
}

public class SalesReportRowDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int NumberOfItems { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class SalesReportDto
{
    public SalesReportSummaryDto Summary { get; set; } = new();
    public PagedResponse<SalesReportRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
    public IReadOnlyList<ChartPointDto> SalesByDay { get; set; } = [];
    public IReadOnlyList<ChartPointDto> SalesByWeek { get; set; } = [];
    public IReadOnlyList<ChartPointDto> SalesByMonth { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> TopSellingProducts { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> SalesByCategory { get; set; } = [];
}
