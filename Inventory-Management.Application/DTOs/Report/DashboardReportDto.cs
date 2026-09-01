namespace Inventory_Management.Application.DTOs.Report;

public class DashboardReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PeriodMetricDto TotalSales { get; set; } = new();
    public PeriodMetricDto TotalPurchases { get; set; } = new();
    public PeriodMetricDto TotalProfit { get; set; } = new();
    public PeriodMetricDto TotalProductsSold { get; set; } = new();
    public PeriodMetricDto TotalCustomers { get; set; } = new();
    public PeriodMetricDto TotalSuppliers { get; set; } = new();
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public IReadOnlyList<ChartPointDto> SalesOverTime { get; set; } = [];
    public IReadOnlyList<ChartPointDto> PurchasesOverTime { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> TopProducts { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> SalesByCategory { get; set; } = [];
}
