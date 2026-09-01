namespace Inventory_Management.Application.DTOs.Supplier;

public class SupplierDetailDto : SupplierDto
{
    public IReadOnlyList<SupplierPurchasedProductDto> PurchasedProducts { get; set; } = [];
    public IReadOnlyList<SupplierPurchaseHistoryDto> PurchaseHistory { get; set; } = [];
}

public class SupplierPurchasedProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
}

public class SupplierPurchaseHistoryDto
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
}
