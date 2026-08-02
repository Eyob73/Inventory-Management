using Inventory_Management.Application.Interfaces.Services;

namespace Inventory_Management.Infrastructure.Services;

public class CurrentTenant : ICurrentTenant
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
