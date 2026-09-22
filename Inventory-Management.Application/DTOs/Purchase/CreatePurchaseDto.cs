using Inventory_Management.Application.DTOs.PurchaseItem;

namespace Inventory_Management.Application.DTOs.Purchase;

public class PurchaseLineInputDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreatePurchaseDto
{
    public Guid? SupplierId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseLineInputDto> Items { get; set; } = [];
}

public class UpdatePurchaseDto
{
    public Guid Id { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseLineInputDto> Items { get; set; } = [];
}

public class PurchaseDto
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = [];
}
