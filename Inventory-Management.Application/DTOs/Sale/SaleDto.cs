namespace Inventory_Management.Application.DTOs.Sale;

public class SaleDto
{
    public Guid Id { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid CustomerId { get; set; }
}
