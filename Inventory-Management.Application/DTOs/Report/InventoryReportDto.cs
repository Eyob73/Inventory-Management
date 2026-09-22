using Inventory_Management.Application.DTOs.Common;

namespace Inventory_Management.Application.DTOs.Report;

public class InventoryReportSummaryDto
{
    public int TotalProducts { get; set; }
    public int TotalStockQuantity { get; set; }
    public decimal InventoryValue { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int CriticalStockProducts { get; set; }
}

public class InventoryReportRowDto
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal InventoryValue { get; set; }
    public string StockStatus { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public class InventoryReportDto
{
    public InventoryReportSummaryDto Summary { get; set; } = new();
    public PagedResponse<InventoryReportRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
}

public class LowStockReportDto
{
    public int OutOfStock { get; set; }
    public int CriticalStock { get; set; }
    public int LowStock { get; set; }
    public PagedResponse<InventoryReportRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
}
