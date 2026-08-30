namespace Inventory_Management.Application.DTOs.SaleItem;

public class CreateSaleItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}
