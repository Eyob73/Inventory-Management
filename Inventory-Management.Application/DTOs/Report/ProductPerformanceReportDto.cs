using Inventory_Management.Application.DTOs.Common;

namespace Inventory_Management.Application.DTOs.Report;

public class ProductPerformanceRowDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal SalesRevenue { get; set; }
    public int PurchaseQuantity { get; set; }
    public int CurrentStock { get; set; }
    public decimal Profit { get; set; }
}

public class ProductPerformanceReportDto
{
    public PagedResponse<ProductPerformanceRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
    public IReadOnlyList<ProductPerformanceRowDto> TopSelling { get; set; } = [];
    public IReadOnlyList<ProductPerformanceRowDto> LeastSelling { get; set; } = [];
    public IReadOnlyList<ProductPerformanceRowDto> MostProfitable { get; set; } = [];
    public IReadOnlyList<ProductPerformanceRowDto> NoSales { get; set; } = [];
}
