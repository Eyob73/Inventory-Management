namespace Inventory_Management.Application.DTOs.Sale;

public class UpdateSaleDto
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid CustomerId { get; set; }
}
