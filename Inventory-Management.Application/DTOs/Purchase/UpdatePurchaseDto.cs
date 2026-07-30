namespace Inventory_Management.Application.DTOs.Purchase;

public class UpdatePurchaseDto
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid SupplierId { get; set; }
}
