namespace Inventory_Management.Application.DTOs.SaleItem;

public class CreateSaleItemDto
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
