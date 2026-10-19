using System.ComponentModel.DataAnnotations;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.DTOs.Payment;

public class EligibleSaleDto
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountAlreadyPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string SaleStatus { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class VerifyPaymentRequestDto
{
    public Guid? SaleId { get; set; }

    public Inventory_Management.Application.DTOs.Sale.CreateSaleDto? Cart { get; set; }

    [Required]
    public Guid PaymentDestinationId { get; set; }

    [Required]
    [MaxLength(500)]
    public string SubmittedReference { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? BankCode { get; set; }

    [MaxLength(30)]
    public string? PayerPhone { get; set; }

    [MaxLength(2000)]
    public string? QrData { get; set; }
}

public class VerifyPaymentResponseDto
{
    public Guid VerificationId { get; set; }
    public Guid? SaleId { get; set; }
    public string? SaleNumber { get; set; } = string.Empty;
    public Guid? PaymentDestinationId { get; set; }
    public string? PaymentDestinationName { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;

    public string SubmittedReference { get; set; } = string.Empty;
    public string? VerifiedReference { get; set; }

    public decimal ExpectedAmount { get; set; }
    public decimal? VerifiedAmount { get; set; }
    public string Currency { get; set; } = "ETB";

    public PaymentVerificationStatus Status { get; set; }
    public string StatusName => Status.ToString();

    public DestinationMatchStatus DestinationMatchStatus { get; set; }
    public string DestinationMatchStatusName => DestinationMatchStatus.ToString();

    public string? PayerName { get; set; }
    public string? PayerAccount { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAccount { get; set; }

    public DateTime? TransactionDateUtc { get; set; }
    public DateTime VerificationTimestamp { get; set; }

    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }

    public bool IsEligibleForRetry { get; set; }
    public bool NeedsManualReview { get; set; }

    public bool SaleUpdated { get; set; }
    public string? NewSaleStatus { get; set; }
    public decimal? NewOutstandingBalance { get; set; }
}

public class PaymentVerificationItemDto
{
    public Guid Id { get; set; }
    public Guid? SaleId { get; set; }
    public string? SaleNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }

    public Guid? PaymentDestinationId { get; set; }
    public string? PaymentDestinationName { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;

    public string SubmittedReference { get; set; } = string.Empty;
    public string? VerifiedReference { get; set; }

    public decimal ExpectedAmount { get; set; }
    public decimal? VerifiedAmount { get; set; }
    public string Currency { get; set; } = "ETB";

    public PaymentVerificationStatus Status { get; set; }
    public string StatusName => Status.ToString();

    public DestinationMatchStatus DestinationMatchStatus { get; set; }
    public string DestinationMatchStatusName => DestinationMatchStatus.ToString();

    public string? PayerName { get; set; }
    public string? PayerAccount { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAccount { get; set; }

    public DateTime? TransactionDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public string? VerifiedByUserId { get; set; }
    public string? VerifiedByUserName { get; set; }

    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }

    public bool? IsManualReviewApproved { get; set; }
    public string? ManualReviewNotes { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

public class PaymentVerificationFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Provider { get; set; }
    public PaymentVerificationStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PaymentVerificationSummaryDto
{
    public int TotalDestinations { get; set; }
    public int ActiveDestinations { get; set; }
    public int VerifiedTransactions { get; set; }
    public int NeedsReviewTransactions { get; set; }
}

public class ManualReviewRequestDto
{
    [Required]
    public bool Approved { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
