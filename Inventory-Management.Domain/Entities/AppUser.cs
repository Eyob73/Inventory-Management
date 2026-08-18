using Inventory_Management.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Inventory_Management.Domain.Entities;

public class AppUser : IdentityUser, IMultiTenant
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}