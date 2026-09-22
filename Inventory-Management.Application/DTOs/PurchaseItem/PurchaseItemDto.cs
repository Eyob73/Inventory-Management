namespace Inventory_Management.Application.DTOs.PurchaseItem;

public class PurchaseItemDto
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsReturnable { get; set; }
    public Guid? BottleTypeId { get; set; }
    public string? BottleTypeName { get; set; }
}

