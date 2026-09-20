using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities;

public enum BottleTransactionType
{
    Received,
    Issued,
    Returned,
    Exchanged,
    Damaged,
    Lost,
    Adjustment,
    Transfer
}

public class BottleTransaction : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    
    public Guid BottleTypeId { get; set; }
    public BottleType? BottleType { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public BottleTransactionType TransactionType { get; set; }
    
    public int Quantity { get; set; }
    public decimal DepositAmount { get; set; }
    
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
