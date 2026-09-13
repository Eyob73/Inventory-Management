using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public bool IsActive { get; set; } = true;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public int LowStockThreshold { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
