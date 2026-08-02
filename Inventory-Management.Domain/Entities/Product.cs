using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities;

public class Product : IMultiTenant, ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int QuantityInStock { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
}
