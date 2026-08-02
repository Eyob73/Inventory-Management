namespace Inventory_Management.Domain.Common;

public interface IMultiTenant
{
    Guid? TenantId { get; set; }
}
