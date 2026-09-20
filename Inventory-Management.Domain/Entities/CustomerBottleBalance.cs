using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities;

public class CustomerBottleBalance : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public Guid BottleTypeId { get; set; }
    public BottleType? BottleType { get; set; }

    public int Balance { get; set; }
    public decimal TotalDeposit { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
