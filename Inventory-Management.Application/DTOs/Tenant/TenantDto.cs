using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.DTOs.Tenant;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public int LowStockThreshold { get; set; }
    public bool EnableBottleManagement { get; set; }
    public bool IsActive { get; set; }
    public TenantStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
