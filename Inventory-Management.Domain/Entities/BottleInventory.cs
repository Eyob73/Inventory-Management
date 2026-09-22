using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities;

public class BottleInventory : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    
    public Guid BottleTypeId { get; set; }
    public BottleType? BottleType { get; set; }

    public int FullBottles { get; set; }
    public int EmptyBottles { get; set; }
    public int DamagedBottles { get; set; }
    public int LostBottles { get; set; }
    public int WithCustomers { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
