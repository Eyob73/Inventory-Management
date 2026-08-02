namespace Inventory_Management.Application.Interfaces.Services;

public interface ICurrentTenant
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}
