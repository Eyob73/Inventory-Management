using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities;

public class PurchaseItem : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Guid PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
