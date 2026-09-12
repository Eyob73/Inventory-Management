using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
