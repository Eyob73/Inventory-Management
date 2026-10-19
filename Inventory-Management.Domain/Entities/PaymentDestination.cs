using Inventory_Management.Domain.Common;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Domain.Entities;

public class PaymentDestination : IMultiTenant, ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public PaymentDestinationType Type { get; set; } = PaymentDestinationType.BankAccount;

    public string? BankCode { get; set; }
    public string? BankName { get; set; }

    public string? AccountNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AccountHolderName { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public string? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
