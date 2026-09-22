using Inventory_Management.Application.DTOs.SaleItem;

namespace Inventory_Management.Application.DTOs.Sale;

public class CreateSaleDto
{
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountReceived { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Notes { get; set; }
    public List<CreateSaleItemDto> Items { get; set; } = new();
}
