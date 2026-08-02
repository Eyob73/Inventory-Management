using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities;

public class Sale : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
