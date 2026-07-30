namespace Inventory_Management.Application.DTOs.Sale;

public class CreateSaleDto
{
    public decimal TotalAmount { get; set; }
    public Guid CustomerId { get; set; }
}
