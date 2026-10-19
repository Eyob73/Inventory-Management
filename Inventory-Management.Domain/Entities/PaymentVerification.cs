using Inventory_Management.Domain.Common;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Domain.Entities;

public class PaymentVerification : IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid? PaymentDestinationId { get; set; }
    public PaymentDestination? PaymentDestination { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;

    public string SubmittedReference { get; set; } = string.Empty;
    public string NormalizedReference { get; set; } = string.Empty;
    public string? VerifiedReference { get; set; }

    public decimal ExpectedAmount { get; set; }
    public decimal? VerifiedAmount { get; set; }
    public string Currency { get; set; } = "ETB";

    public PaymentVerificationStatus Status { get; set; } = PaymentVerificationStatus.Pending;
    public DestinationMatchStatus DestinationMatchStatus { get; set; } = DestinationMatchStatus.UnableToConfirm;

    public string? PayerName { get; set; }
    public string? PayerAccount { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAccount { get; set; }

    public DateTime? TransactionDateUtc { get; set; }
    public string? VerifiedByUserId { get; set; }
    public string? VerifiedByUserName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? SourceUrl { get; set; }

    // Manual review fields
    public bool? IsManualReviewApproved { get; set; }
    public string? ManualReviewNotes { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
