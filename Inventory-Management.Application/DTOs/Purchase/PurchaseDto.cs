namespace Inventory_Management.Application.DTOs.Purchase;

public class PurchaseDto
{
    public Guid Id { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid SupplierId { get; set; }
}
