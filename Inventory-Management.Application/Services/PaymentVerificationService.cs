using Inventory_Management.Application.Common;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Payment;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory_Management.Application.Services;

public class PaymentVerificationService : IPaymentVerificationService
{
    private readonly IGenericRepository<PaymentVerification> _verificationRepository;
    private readonly IGenericRepository<PaymentDestination> _destinationRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChekiVerificationService _chekiService;
    private readonly ILogger<PaymentVerificationService> _logger;

    public PaymentVerificationService(
        IGenericRepository<PaymentVerification> verificationRepository,
        IGenericRepository<PaymentDestination> destinationRepository,
        ISaleRepository saleRepository,
        IGenericRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IChekiVerificationService chekiService,
        ILogger<PaymentVerificationService> logger)
    {
        _verificationRepository = verificationRepository;
        _destinationRepository = destinationRepository;
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _chekiService = chekiService;
        _logger = logger;
    }

    public async Task<VerifyPaymentResponseDto> VerifyCustomerPaymentAsync(
        VerifyPaymentRequestDto dto,
        string? currentUserId,
        string? currentUserName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);


        if (dto.PaymentDestinationId == Guid.Empty)
            throw new ArgumentException("A valid payment destination must be selected.", nameof(dto.PaymentDestinationId));

        if (string.IsNullOrWhiteSpace(dto.SubmittedReference) && !string.IsNullOrWhiteSpace(dto.QrData))
        {
            dto.SubmittedReference = PaymentDestinationSecurity.NormalizeReference(dto.QrData);
        }

        if (string.IsNullOrWhiteSpace(dto.SubmittedReference))
            throw new ArgumentException("Transaction reference or receipt link is required.", nameof(dto.SubmittedReference));

        // SSRF protection and banking domain validation for receipt URLs
        if (dto.SubmittedReference.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            dto.SubmittedReference.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!PaymentDestinationSecurity.ValidateReceiptUrl(dto.SubmittedReference, out var ssrfError))
            {
                throw new ArgumentException(ssrfError ?? "Invalid receipt URL format or unauthorized banking domain.", nameof(dto.SubmittedReference));
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.QrData) &&
            (dto.QrData.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             dto.QrData.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            if (!PaymentDestinationSecurity.ValidateReceiptUrl(dto.QrData, out var qrSsrfError))
            {
                throw new ArgumentException(qrSsrfError ?? "Invalid receipt QR URL format or unauthorized banking domain.", nameof(dto.QrData));
            }
        }

        Sale? sale = null;
        decimal expectedAmount = 0m;

        // 1. Load Sale and verify tenant ownership OR recalculate authoritatively from Cart
        if (dto.SaleId.HasValue && dto.SaleId.Value != Guid.Empty)
        {
            sale = await _saleRepository.Query()
                .FirstOrDefaultAsync(s => s.Id == dto.SaleId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Sale with ID '{dto.SaleId}' was not found.");

            if (sale.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Cannot verify payment for cancelled sale #{sale.SaleNumber}.");

            expectedAmount = Math.Max(0m, sale.TotalAmount - sale.AmountReceived);
            if (expectedAmount <= 0)
                throw new InvalidOperationException($"Sale #{sale.SaleNumber} has already been fully paid.");
        }
        else if (dto.Cart != null)
        {
            if (dto.Cart.Items == null || dto.Cart.Items.Count == 0)
                throw new InvalidOperationException("Checkout cart cannot be empty.");

            // Authoritative backend recalculation of cart total from DB product prices
            decimal subtotal = 0m;
            foreach (var item in dto.Cart.Items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException($"Invalid item quantity: {item.Quantity}. Quantity must be greater than zero.");

                var product = await _productRepository.GetByIdAsync(item.ProductId)
                    ?? throw new KeyNotFoundException($"Product with ID '{item.ProductId}' was not found.");

                subtotal += item.Quantity * product.Price;
            }

            decimal discount = Math.Max(0m, dto.Cart.DiscountAmount);
            decimal tax = Math.Max(0m, dto.Cart.TaxAmount);
            decimal additionalDeposit = Math.Max(0m, dto.Cart.AdditionalBottleDeposit);
            expectedAmount = Math.Max(0m, subtotal - discount + tax + additionalDeposit);

            if (expectedAmount <= 0)
                throw new InvalidOperationException("Calculated sale total must be greater than zero for electronic payment verification.");
        }

        // 2. Load Payment Destination and verify tenant ownership and active status
        var destination = await _destinationRepository.Query()
            .FirstOrDefaultAsync(d => d.Id == dto.PaymentDestinationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment destination with ID '{dto.PaymentDestinationId}' was not found.");

        if (!destination.IsActive)
            throw new InvalidOperationException($"Payment destination '{destination.Name}' is currently inactive.");

        var bankCode = !string.IsNullOrWhiteSpace(dto.BankCode)
            ? dto.BankCode.Trim().ToLowerInvariant()
            : (destination.BankCode ?? "cbe").Trim().ToLowerInvariant();

        var providerName = destination.BankName ?? destination.Name;
        var normalizedRef = PaymentDestinationSecurity.NormalizeReference(dto.SubmittedReference);

        // 3. Concurrency / Duplicate reference check against previously applied transactions
        var duplicate = await _verificationRepository.Query()
            .Include(v => v.Sale)
            .FirstOrDefaultAsync(v =>
                v.BankCode == bankCode &&
                v.NormalizedReference == normalizedRef &&
                (v.Status == PaymentVerificationStatus.Verified || v.IsManualReviewApproved == true),
                cancellationToken);

        if (duplicate != null)
        {
            var duplicateAttempt = new PaymentVerification
            {
                Id = Guid.NewGuid(),
                SaleId = sale?.Id,
                PaymentDestinationId = destination.Id,
                Provider = providerName,
                BankCode = bankCode,
                SubmittedReference = dto.SubmittedReference.Trim(),
                NormalizedReference = normalizedRef,
                ExpectedAmount = expectedAmount,
                Currency = "ETB",
                Status = PaymentVerificationStatus.Rejected,
                DestinationMatchStatus = DestinationMatchStatus.UnableToConfirm,
                VerifiedByUserId = currentUserId,
                VerifiedByUserName = currentUserName,
                CreatedAtUtc = DateTime.UtcNow,
                FailureCode = "DUPLICATE_REFERENCE",
                FailureMessage = duplicate.Sale != null
                    ? $"Transaction reference '{normalizedRef}' was already verified and applied to Sale #{duplicate.Sale.SaleNumber}."
                    : $"Transaction reference '{normalizedRef}' was already verified and applied to another transaction."
            };

            await _verificationRepository.AddAsync(duplicateAttempt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToResponseDto(duplicateAttempt, sale, destination, false);
        }

        // 4. Create and persist initial pending verification attempt
        var verification = new PaymentVerification
        {
            Id = Guid.NewGuid(),
            SaleId = sale?.Id,
            PaymentDestinationId = destination.Id,
            Provider = providerName,
            BankCode = bankCode,
            SubmittedReference = dto.SubmittedReference.Trim(),
            NormalizedReference = normalizedRef,
            ExpectedAmount = expectedAmount,
            Currency = "ETB",
            Status = PaymentVerificationStatus.Pending,
            DestinationMatchStatus = DestinationMatchStatus.UnableToConfirm,
            VerifiedByUserId = currentUserId,
            VerifiedByUserName = currentUserName,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _verificationRepository.AddAsync(verification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Call Cheki (database transaction is NOT held open during external HTTP call)
        var chekiRequest = new ChekiVerifyRequest
        {
            Bank = bankCode,
            Reference = dto.SubmittedReference.Trim(),
            AccountNumber = destination.AccountNumber,
            PayerPhone = dto.PayerPhone,
            QrData = dto.QrData
        };

        ChekiVerifyResponse chekiResponse;
        try
        {
            chekiResponse = await _chekiService.VerifyPaymentAsync(chekiRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during Cheki verification call.");
            chekiResponse = new ChekiVerifyResponse
            {
                Success = false,
                Verified = false,
                Error = "Failed to contact payment verification gateway.",
                StatusCode = 503
            };
        }

        verification.UpdatedAtUtc = DateTime.UtcNow;

        // 6. Handle Telebirr cloud IP blocks (Ethio Telecom blocks foreign/cloud IPs from querying receipt gateway)
        if (IsTelebirrCloudIpBlocked(chekiResponse, destination))
        {
            var receiptUrl = ResolveTelebirrReceiptUrl(chekiResponse, normalizedRef);
            verification.SourceUrl = receiptUrl;
            verification.VerifiedReference = normalizedRef;
            verification.Status = PaymentVerificationStatus.NeedsManualReview;
            verification.FailureCode = "TELEBIRR_DIRECT_VERIFICATION_REQUIRED";
            verification.FailureMessage = "Ethio Telecom blocks cloud server verification. Please open the official Telebirr receipt directly to verify details, then approve via Manual Review.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false, isEligibleForRetry: false, needsManualReview: true);
        }

        // 7. Handle transient errors or rate limits
        if (chekiResponse.IsRateLimited || chekiResponse.StatusCode == 429 || chekiResponse.StatusCode >= 500)
        {
            verification.Status = PaymentVerificationStatus.Failed;
            verification.FailureCode = chekiResponse.IsRateLimited ? "RATE_LIMITED" : "SERVICE_UNAVAILABLE";
            verification.FailureMessage = chekiResponse.Error ?? "Verification service is temporarily unavailable. Please retry.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false, isEligibleForRetry: true);
        }

        // 8. Check if provider returned confirmed verification
        if (!chekiResponse.Success || !chekiResponse.Verified)
        {
            if (IsTelebirrCloudIpBlocked(chekiResponse, destination))
            {
                var receiptUrl = ResolveTelebirrReceiptUrl(chekiResponse, normalizedRef);
                verification.SourceUrl = receiptUrl;
                verification.VerifiedReference = normalizedRef;
                verification.Status = PaymentVerificationStatus.NeedsManualReview;
                verification.FailureCode = "TELEBIRR_DIRECT_VERIFICATION_REQUIRED";
                verification.FailureMessage = "Ethio Telecom blocks cloud server verification. Please open the official Telebirr receipt directly to verify details, then approve via Manual Review.";

                await _verificationRepository.UpdateAsync(verification);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return MapToResponseDto(verification, sale, destination, false, isEligibleForRetry: false, needsManualReview: true);
            }

            verification.Status = PaymentVerificationStatus.Rejected;
            verification.FailureCode = "PROVIDER_NOT_VERIFIED";
            verification.FailureMessage = chekiResponse.Error ?? "The transaction reference could not be verified by the bank.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false);
        }

        // 8. Populate returned data from Cheki
        verification.VerifiedReference = chekiResponse.Reference ?? normalizedRef;
        verification.VerifiedAmount = chekiResponse.Amount;
        verification.Currency = chekiResponse.Currency ?? "ETB";
        verification.PayerName = chekiResponse.SenderName;
        verification.PayerAccount = chekiResponse.SenderAccount;
        verification.ReceiverName = chekiResponse.ReceiverName;
        verification.ReceiverAccount = chekiResponse.ReceiverAccount;
        verification.SourceUrl = chekiResponse.SourceUrl;

        if (DateTime.TryParse(chekiResponse.Date, out var txDate))
        {
            verification.TransactionDateUtc = txDate.ToUniversalTime();
        }

        // 9. Validate Currency
        if (!verification.Currency.Equals("ETB", StringComparison.OrdinalIgnoreCase))
        {
            verification.Status = PaymentVerificationStatus.AmountMismatch;
            verification.FailureCode = "INVALID_CURRENCY";
            verification.FailureMessage = $"Transaction currency '{verification.Currency}' does not match expected ETB currency.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false);
        }

        // 10. Validate Amount
        decimal verifiedAmt = verification.VerifiedAmount ?? 0m;
        if (verifiedAmt <= 0)
        {
            verification.Status = PaymentVerificationStatus.AmountMismatch;
            verification.FailureCode = "ZERO_AMOUNT";
            verification.FailureMessage = "Verified transaction amount must be greater than zero.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false);
        }

        // Allowed overpayment margin (up to 500 ETB above expected amount, above only)
        const decimal OverpaymentMargin = 500m;

        // If verified amount exceeds expected amount plus allowed margin (above only)
        if (expectedAmount > 0m && verifiedAmt > (expectedAmount + OverpaymentMargin))
        {
            verification.Status = PaymentVerificationStatus.AmountMismatch;
            verification.FailureCode = "AMOUNT_EXCEEDS_BALANCE";
            verification.FailureMessage = $"Verified amount of {verifiedAmt:N2} ETB exceeds the allowed margin (expected {expectedAmount:N2} ETB + {OverpaymentMargin:N2} ETB margin).";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false, needsManualReview: true);
        }

        // For cart checkout (POS), underpayment is not allowed (margin is above only)
        if (dto.Cart != null && expectedAmount > 0m && verifiedAmt < expectedAmount)
        {
            verification.Status = PaymentVerificationStatus.AmountMismatch;
            verification.FailureCode = "AMOUNT_UNDERPAYMENT";
            verification.FailureMessage = $"Verified amount of {verifiedAmt:N2} ETB is less than required cart total of {expectedAmount:N2} ETB. No underpayment margin is allowed.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false);
        }

        // For standalone verification without a prior sale or cart, authoritative expected amount equals verified amount
        if (expectedAmount == 0m)
        {
            verification.ExpectedAmount = verifiedAmt;
        }

        // 11. Destination Matching Evaluation
        var (matchStatus, matchMessage) = PaymentDestinationSecurity.EvaluateDestinationMatch(
            destination,
            chekiResponse.ReceiverAccount,
            chekiResponse.ReceiverName);

        verification.DestinationMatchStatus = matchStatus;

        if (matchStatus == DestinationMatchStatus.Mismatched)
        {
            verification.Status = PaymentVerificationStatus.DestinationMismatch;
            verification.FailureCode = "DESTINATION_MISMATCH";
            verification.FailureMessage = matchMessage ?? "Receiving account does not match the registered business destination.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false, needsManualReview: true);
        }

        if (matchStatus == DestinationMatchStatus.UnableToConfirm)
        {
            verification.Status = PaymentVerificationStatus.UnableToConfirmDestination;
            verification.FailureCode = "UNABLE_TO_CONFIRM_DESTINATION";
            verification.FailureMessage = matchMessage ?? "Transaction verified, but receiving account details could not be matched. Manual review required.";

            await _verificationRepository.UpdateAsync(verification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToResponseDto(verification, sale, destination, false, needsManualReview: true);
        }

        // 12. All mandatory checks passed: Finalize atomically
        verification.Status = PaymentVerificationStatus.Verified;
        verification.FailureCode = null;
        verification.FailureMessage = null;

        if (sale != null)
        {
            // Atomically apply payment to existing sale
            sale.AmountReceived += verifiedAmt;
            if (sale.AmountReceived >= sale.TotalAmount)
            {
                sale.Status = "Completed";
            }
            else
            {
                sale.Status = "PartiallyPaid";
            }

            sale.PaymentMethod = destination.Type == PaymentDestinationType.Telebirr
                ? "Telebirr"
                : (destination.BankName ?? "Bank Transfer");

            await _saleRepository.UpdateAsync(sale);
        }

        await _verificationRepository.UpdateAsync(verification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(verification, sale, destination, sale != null);
    }

    public async Task<PagedResponse<PaymentVerificationItemDto>> GetVerificationsAsync(
        PaymentVerificationFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new PaymentVerificationFilterDto();

        var query = _verificationRepository.Query()
            .Include(v => v.Sale)
            .Include(v => v.PaymentDestination)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(v =>
                v.NormalizedReference.ToLower().Contains(search) ||
                v.SubmittedReference.ToLower().Contains(search) ||
                (v.Sale != null && v.Sale.SaleNumber.ToLower().Contains(search)) ||
                (v.PayerName != null && v.PayerName.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Provider))
        {
            var prov = filter.Provider.Trim().ToLowerInvariant();
            query = query.Where(v => v.Provider.ToLower().Contains(prov) || v.BankCode.ToLower() == prov);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(v => v.Status == filter.Status.Value);
        }

        if (filter.StartDate.HasValue)
        {
            var startUtc = filter.StartDate.Value.ToUniversalTime();
            query = query.Where(v => v.CreatedAtUtc >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = filter.EndDate.Value.ToUniversalTime();
            query = query.Where(v => v.CreatedAtUtc <= endUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageIndex = Math.Max(1, filter.PageIndex);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(v => v.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new PaymentVerificationItemDto
            {
                Id = v.Id,
                SaleId = v.SaleId,
                SaleNumber = v.Sale != null ? v.Sale.SaleNumber : string.Empty,
                CustomerName = v.Sale != null ? v.Sale.CustomerName : null,
                PaymentDestinationId = v.PaymentDestinationId,
                PaymentDestinationName = v.PaymentDestination != null ? v.PaymentDestination.Name : null,
                Provider = v.Provider,
                BankCode = v.BankCode,
                SubmittedReference = v.SubmittedReference,
                VerifiedReference = v.VerifiedReference,
                ExpectedAmount = v.ExpectedAmount,
                VerifiedAmount = v.VerifiedAmount,
                Currency = v.Currency,
                Status = v.Status,
                DestinationMatchStatus = v.DestinationMatchStatus,
                PayerName = v.PayerName,
                PayerAccount = PaymentDestinationSecurity.MaskAccountNumber(v.PayerAccount),
                ReceiverName = v.ReceiverName,
                ReceiverAccount = PaymentDestinationSecurity.MaskAccountNumber(v.ReceiverAccount),
                TransactionDateUtc = v.TransactionDateUtc,
                CreatedAtUtc = v.CreatedAtUtc,
                VerifiedByUserId = v.VerifiedByUserId,
                VerifiedByUserName = v.VerifiedByUserName,
                FailureCode = v.FailureCode,
                FailureMessage = v.FailureMessage,
                IsManualReviewApproved = v.IsManualReviewApproved,
                ManualReviewNotes = v.ManualReviewNotes,
                ReviewedByUserName = v.ReviewedByUserName,
                ReviewedAtUtc = v.ReviewedAtUtc,
                SourceUrl = v.SourceUrl
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<PaymentVerificationItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<PaymentVerificationItemDto> GetVerificationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var v = await _verificationRepository.Query()
            .Include(v => v.Sale)
            .Include(v => v.PaymentDestination)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Verification record with ID '{id}' was not found.");

        return new PaymentVerificationItemDto
        {
            Id = v.Id,
            SaleId = v.SaleId,
            SaleNumber = v.Sale != null ? v.Sale.SaleNumber : string.Empty,
            CustomerName = v.Sale != null ? v.Sale.CustomerName : null,
            PaymentDestinationId = v.PaymentDestinationId,
            PaymentDestinationName = v.PaymentDestination != null ? v.PaymentDestination.Name : null,
            Provider = v.Provider,
            BankCode = v.BankCode,
            SubmittedReference = v.SubmittedReference,
            VerifiedReference = v.VerifiedReference,
            ExpectedAmount = v.ExpectedAmount,
            VerifiedAmount = v.VerifiedAmount,
            Currency = v.Currency,
            Status = v.Status,
            DestinationMatchStatus = v.DestinationMatchStatus,
            PayerName = v.PayerName,
            PayerAccount = PaymentDestinationSecurity.MaskAccountNumber(v.PayerAccount),
            ReceiverName = v.ReceiverName,
            ReceiverAccount = PaymentDestinationSecurity.MaskAccountNumber(v.ReceiverAccount),
            TransactionDateUtc = v.TransactionDateUtc,
            CreatedAtUtc = v.CreatedAtUtc,
            VerifiedByUserId = v.VerifiedByUserId,
            VerifiedByUserName = v.VerifiedByUserName,
            FailureCode = v.FailureCode,
            FailureMessage = v.FailureMessage,
            IsManualReviewApproved = v.IsManualReviewApproved,
            ManualReviewNotes = v.ManualReviewNotes,
            ReviewedByUserName = v.ReviewedByUserName,
            ReviewedAtUtc = v.ReviewedAtUtc,
            SourceUrl = v.SourceUrl
        };
    }

    public async Task<PaymentVerificationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalDestinations = await _destinationRepository.Query().CountAsync(cancellationToken);
        var activeDestinations = await _destinationRepository.Query().CountAsync(d => d.IsActive, cancellationToken);
        var verifiedTransactions = await _verificationRepository.Query().CountAsync(v => v.Status == PaymentVerificationStatus.Verified, cancellationToken);
        var needsReviewTransactions = await _verificationRepository.Query().CountAsync(v =>
            v.Status == PaymentVerificationStatus.NeedsManualReview ||
            v.Status == PaymentVerificationStatus.UnableToConfirmDestination ||
            v.Status == PaymentVerificationStatus.AmountMismatch ||
            v.Status == PaymentVerificationStatus.DestinationMismatch,
            cancellationToken);

        return new PaymentVerificationSummaryDto
        {
            TotalDestinations = totalDestinations,
            ActiveDestinations = activeDestinations,
            VerifiedTransactions = verifiedTransactions,
            NeedsReviewTransactions = needsReviewTransactions
        };
    }

    public async Task<List<EligibleSaleDto>> GetEligibleSalesAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _saleRepository.Query()
            .AsNoTracking()
            .Where(s => s.Status.ToLower() != "cancelled" &&
                        (s.TotalAmount - s.AmountReceived) > 0);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(s =>
                s.SaleNumber.ToLower().Contains(term) ||
                (s.CustomerName != null && s.CustomerName.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(s => s.SaleDate)
            .Take(50)
            .Select(s => new EligibleSaleDto
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                CustomerId = s.CustomerId,
                CustomerName = s.CustomerName,
                TotalAmount = s.TotalAmount,
                AmountAlreadyPaid = s.AmountReceived,
                OutstandingBalance = s.TotalAmount - s.AmountReceived,
                SaleStatus = s.Status,
                SaleDate = s.SaleDate,
                PaymentMethod = s.PaymentMethod
            })
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<VerifyPaymentResponseDto> RetryVerificationAsync(
        Guid id,
        string? currentUserId,
        string? currentUserName,
        CancellationToken cancellationToken = default)
    {
        var verification = await _verificationRepository.Query()
            .Include(v => v.Sale)
            .Include(v => v.PaymentDestination)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Verification record with ID '{id}' was not found.");

        if (verification.Status != PaymentVerificationStatus.Failed)
        {
            throw new InvalidOperationException($"Only failed verification attempts can be retried. Current status is {verification.Status}.");
        }


        var verifyReq = new VerifyPaymentRequestDto
        {
            SaleId = verification.SaleId,
            PaymentDestinationId = verification.PaymentDestinationId ?? Guid.Empty,
            SubmittedReference = verification.SubmittedReference,
            BankCode = verification.BankCode
        };

        return await VerifyCustomerPaymentAsync(verifyReq, currentUserId, currentUserName, cancellationToken);
    }

    public async Task<PaymentVerificationItemDto> ProcessManualReviewAsync(
        Guid id,
        ManualReviewRequestDto dto,
        string? currentUserId,
        string? currentUserName,
        string? currentRole,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrEmpty(currentRole) ||
            (!currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
             !currentRole.Equals("Manager", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Only Administrators and Managers are authorized to perform manual review on payment transactions.");
        }

        var verification = await _verificationRepository.Query()
            .Include(v => v.Sale)
            .Include(v => v.PaymentDestination)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Verification record with ID '{id}' was not found.");

        if (verification.Status == PaymentVerificationStatus.Verified)
        {
            throw new InvalidOperationException("This transaction has already been verified and applied.");
        }

        var sale = verification.Sale
            ?? throw new InvalidOperationException("Associated sale was not found.");

        verification.IsManualReviewApproved = dto.Approved;
        verification.ManualReviewNotes = dto.Notes?.Trim();
        verification.ReviewedByUserId = currentUserId;
        verification.ReviewedByUserName = currentUserName;
        verification.ReviewedAtUtc = DateTime.UtcNow;
        verification.UpdatedAtUtc = DateTime.UtcNow;

        if (dto.Approved)
        {
            decimal verifiedAmt = verification.VerifiedAmount ?? verification.ExpectedAmount;
            if (verifiedAmt <= 0)
                throw new InvalidOperationException("Cannot approve verification with zero verified amount.");

            verification.Status = PaymentVerificationStatus.Verified;

            // Atomically update sale
            sale.AmountReceived += verifiedAmt;
            if (sale.AmountReceived >= sale.TotalAmount)
            {
                sale.Status = "Completed";
            }
            else
            {
                sale.Status = "PartiallyPaid";
            }

            if (verification.PaymentDestination != null)
            {
                sale.PaymentMethod = verification.PaymentDestination.Type == PaymentDestinationType.Telebirr
                    ? "Telebirr"
                    : (verification.PaymentDestination.BankName ?? "Bank Transfer");
            }

            await _saleRepository.UpdateAsync(sale);
        }
        else
        {
            verification.Status = PaymentVerificationStatus.Rejected;
            verification.FailureCode = "MANUAL_REVIEW_REJECTED";
            verification.FailureMessage = dto.Notes ?? "Transaction was rejected during manual review.";
        }

        await _verificationRepository.UpdateAsync(verification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetVerificationByIdAsync(id, cancellationToken);
    }

    private static VerifyPaymentResponseDto MapToResponseDto(
        PaymentVerification v,
        Sale? sale,
        PaymentDestination destination,
        bool saleUpdated,
        bool isEligibleForRetry = false,
        bool needsManualReview = false)
    {
        return new VerifyPaymentResponseDto
        {
            VerificationId = v.Id,
            SaleId = sale?.Id,
            SaleNumber = sale?.SaleNumber,
            PaymentDestinationId = destination.Id,
            PaymentDestinationName = destination.Name,
            Provider = v.Provider,
            BankCode = v.BankCode,
            SubmittedReference = v.SubmittedReference,
            VerifiedReference = v.VerifiedReference,
            ExpectedAmount = v.ExpectedAmount,
            VerifiedAmount = v.VerifiedAmount,
            Currency = v.Currency,
            Status = v.Status,
            DestinationMatchStatus = v.DestinationMatchStatus,
            PayerName = v.PayerName,
            PayerAccount = PaymentDestinationSecurity.MaskAccountNumber(v.PayerAccount),
            ReceiverName = v.ReceiverName,
            ReceiverAccount = PaymentDestinationSecurity.MaskAccountNumber(v.ReceiverAccount),
            TransactionDateUtc = v.TransactionDateUtc,
            VerificationTimestamp = v.UpdatedAtUtc ?? v.CreatedAtUtc,
            FailureCode = v.FailureCode,
            FailureMessage = v.FailureMessage,
            IsEligibleForRetry = isEligibleForRetry,
            NeedsManualReview = needsManualReview ||
                                v.Status == PaymentVerificationStatus.NeedsManualReview ||
                                v.Status == PaymentVerificationStatus.UnableToConfirmDestination ||
                                v.Status == PaymentVerificationStatus.DestinationMismatch ||
                                v.Status == PaymentVerificationStatus.AmountMismatch,
            SaleUpdated = saleUpdated,
            NewSaleStatus = sale?.Status,
            NewOutstandingBalance = sale != null ? Math.Max(0m, sale.TotalAmount - sale.AmountReceived) : null,
            SourceUrl = v.SourceUrl
        };
    }

    private static bool IsTelebirrCloudIpBlocked(ChekiVerifyResponse response, PaymentDestination destination)
    {
        var err = response.Error ?? string.Empty;

        // Directly checks for Cheki's Telebirr cloud IP block indicator
        if (err.Contains("blocks cloud IPs", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("transactioninfo.ethiotelecom.et", StringComparison.OrdinalIgnoreCase) ||
            (err.Contains("can't reach this bank", StringComparison.OrdinalIgnoreCase) && err.Contains("Telebirr", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        bool isTelebirr = string.Equals(destination.BankCode, "TELEBIRR", StringComparison.OrdinalIgnoreCase) ||
                          (!string.IsNullOrEmpty(destination.Name) && destination.Name.Contains("telebirr", StringComparison.OrdinalIgnoreCase)) ||
                          destination.Type == PaymentDestinationType.Telebirr;

        if (isTelebirr && (err.Contains("cloud IP", StringComparison.OrdinalIgnoreCase) ||
                           err.Contains("Our server can't reach", StringComparison.OrdinalIgnoreCase) ||
                           response.StatusCode == 503 ||
                           response.StatusCode == 502))
        {
            return true;
        }

        return false;
    }

    private static string ResolveTelebirrReceiptUrl(ChekiVerifyResponse response, string reference)
    {
        if (!string.IsNullOrWhiteSpace(response.SourceUrl) &&
            response.SourceUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return response.SourceUrl;
        }

        var err = response.Error ?? string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(
            err,
            @"https?://transactioninfo\.ethiotelecom\.et/receipt/[a-zA-Z0-9_-]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Value;
        }

        return $"https://transactioninfo.ethiotelecom.et/receipt/{reference.Trim()}";
    }
}
