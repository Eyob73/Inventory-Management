namespace Inventory_Management.Application.DTOs.Tenant;

public class UpdateMyTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public int? LowStockThreshold { get; set; }
    public bool? EnableBottleManagement { get; set; }
}
