using Inventory_Management.Domain.Common;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Domain.Entities;

public class InventoryTransaction : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public int PreviousQuantity { get; set; }
    public int NewQuantity { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
