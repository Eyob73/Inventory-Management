using Inventory_Management.Application.DTOs.Common;

namespace Inventory_Management.Application.DTOs.Report;

public class CustomerReportRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int NumberOfPurchases { get; set; }
    public decimal TotalAmountSpent { get; set; }
    public decimal AveragePurchaseValue { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}

public class CustomerReportDto
{
    public PagedResponse<CustomerReportRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
    public IReadOnlyList<CustomerReportRowDto> TopBySpending { get; set; } = [];
    public IReadOnlyList<CustomerReportRowDto> MostFrequent { get; set; } = [];
    public IReadOnlyList<CustomerReportRowDto> NoRecentPurchases { get; set; } = [];
}

public class SupplierReportRowDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public int NumberOfPurchases { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public int ProductsSupplied { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}

public class SupplierPurchaseHistoryRowDto
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
}

public class SupplierReportDto
{
    public PagedResponse<SupplierReportRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
    public IReadOnlyList<SupplierReportRowDto> TopByAmount { get; set; } = [];
    public IReadOnlyList<SupplierReportRowDto> MostFrequent { get; set; } = [];
    public IReadOnlyList<SupplierPurchaseHistoryRowDto> PurchaseHistory { get; set; } = [];
}

public class ProfitReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossProfitMargin { get; set; }
    public string CostBasis { get; set; } = "Weighted average of completed purchase costs; product cost used when no purchase history exists.";
    public IReadOnlyList<ChartPointDto> ProfitOverTime { get; set; } = [];
}

public class StockMovementRowDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Product { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceNumber { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
}

public class StockMovementReportDto
{
    public PagedResponse<StockMovementRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
}
