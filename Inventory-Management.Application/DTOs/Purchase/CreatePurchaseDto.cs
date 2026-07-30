namespace Inventory_Management.Application.DTOs.Purchase;

public class CreatePurchaseDto
{
    public decimal TotalAmount { get; set; }
    public Guid SupplierId { get; set; }
}
