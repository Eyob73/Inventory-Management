using Inventory_Management.Application.DTOs.Common;

namespace Inventory_Management.Application.DTOs.Report;

public class PurchasesReportSummaryDto
{
    public decimal TotalPurchaseAmount { get; set; }
    public int NumberOfPurchases { get; set; }
    public int TotalProductsPurchased { get; set; }
    public decimal AveragePurchaseValue { get; set; }
}

public class PurchasesReportRowDto
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public int NumberOfItems { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PurchasesReportDto
{
    public PurchasesReportSummaryDto Summary { get; set; } = new();
    public PagedResponse<PurchasesReportRowDto> Table { get; set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 20
    };
    public IReadOnlyList<ChartPointDto> PurchasesOverTime { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> PurchasesBySupplier { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> MostPurchasedProducts { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> PurchaseAmountByCategory { get; set; } = [];
}
