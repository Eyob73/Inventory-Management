using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities;

public class Sale : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? CustomerName { get; set; }

    public string? UserId { get; set; }
    public AppUser? User { get; set; }
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
