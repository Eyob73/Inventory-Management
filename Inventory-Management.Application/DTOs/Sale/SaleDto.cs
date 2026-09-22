using Inventory_Management.Application.DTOs.SaleItem;

namespace Inventory_Management.Application.DTOs.Sale;

public class SaleDto
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    
    public string? UserId { get; set; }
    public string? CashierName { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Completed";

    public DateTime CreatedAt { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
}
