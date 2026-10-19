using System.Security.Claims;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Payment;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Admin,Manager,Sales")]
[Tags("Payment Verification")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentVerificationService _verificationService;
    private readonly UserManager<AppUser> _userManager;

    public PaymentsController(
        IPaymentVerificationService verificationService,
        UserManager<AppUser> userManager)
    {
        _verificationService = verificationService;
        _userManager = userManager;
    }

    [HttpPost("verify")]
    [EndpointSummary("Verify customer payment with Cheki.et")]
    [EndpointDescription("Validates a submitted transaction reference or receipt link against the bank provider and matches destination details before applying the payment to the sale.")]
    [ProducesResponseType(typeof(VerifyPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerifyPaymentResponseDto>> Verify([FromBody] VerifyPaymentRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? userName = null;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    userName = !string.IsNullOrWhiteSpace(user.FirstName)
                        ? $"{user.FirstName} {user.LastName}".Trim()
                        : user.UserName;
                }
            }

            var result = await _verificationService.VerifyCustomerPaymentAsync(dto, userId, userName, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [HttpGet("verifications")]
    [EndpointSummary("List verification transaction history")]
    [EndpointDescription("Retrieves paged verification transaction history with filters for reference, provider, status, and dates.")]
    [ProducesResponseType(typeof(PagedResponse<PaymentVerificationItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PaymentVerificationItemDto>>> GetVerifications(
        [FromQuery] PaymentVerificationFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _verificationService.GetVerificationsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("verifications/{id:guid}")]
    [EndpointSummary("Get verification transaction details")]
    [ProducesResponseType(typeof(PaymentVerificationItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentVerificationItemDto>> GetVerificationById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _verificationService.GetVerificationByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpGet("verifications/summary")]
    [EndpointSummary("Get payment verification summary stats")]
    [ProducesResponseType(typeof(PaymentVerificationSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentVerificationSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _verificationService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("eligible-sales")]
    [EndpointSummary("List eligible sales awaiting payment")]
    [EndpointDescription("Retrieves sales with outstanding balances eligible for payment verification.")]
    [ProducesResponseType(typeof(List<EligibleSaleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EligibleSaleDto>>> GetEligibleSales([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var sales = await _verificationService.GetEligibleSalesAsync(search, cancellationToken);
        return Ok(sales);
    }

    [HttpPost("verifications/{id:guid}/retry")]
    [EndpointSummary("Retry a transient failed verification attempt")]
    [ProducesResponseType(typeof(VerifyPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VerifyPaymentResponseDto>> Retry(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;
            var result = await _verificationService.RetryVerificationAsync(id, userId, userName, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [HttpPost("verifications/{id:guid}/manual-review")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Approve or reject a transaction requiring manual review")]
    [EndpointDescription("Authorized review action to manually confirm or reject a payment that could not be automatically confirmed by the destination matcher.")]
    [ProducesResponseType(typeof(PaymentVerificationItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentVerificationItemDto>> ProcessManualReview(
        Guid id,
        [FromBody] ManualReviewRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;
            var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : null;

            var result = await _verificationService.ProcessManualReviewAsync(
                id,
                dto,
                userId,
                userName,
                role,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }
}
