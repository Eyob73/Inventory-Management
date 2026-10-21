using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Payment;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IPaymentVerificationService
{
    Task<VerifyPaymentResponseDto> VerifyCustomerPaymentAsync(
        VerifyPaymentRequestDto dto,
        string? currentUserId,
        string? currentUserName,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<PaymentVerificationItemDto>> GetVerificationsAsync(
        PaymentVerificationFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<PaymentVerificationItemDto> GetVerificationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PaymentVerificationSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<List<EligibleSaleDto>> GetEligibleSalesAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<VerifyPaymentResponseDto> RetryVerificationAsync(
        Guid id,
        string? currentUserId,
        string? currentUserName,
        CancellationToken cancellationToken = default);

    Task<PaymentVerificationItemDto> ProcessManualReviewAsync(
        Guid id,
        ManualReviewRequestDto dto,
        string? currentUserId,
        string? currentUserName,
        string? currentRole,
        CancellationToken cancellationToken = default);
}
