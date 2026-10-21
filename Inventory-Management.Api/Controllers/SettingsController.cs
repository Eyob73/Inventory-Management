using Inventory_Management.Application.DTOs.Settings;
using Inventory_Management.Application.Features.Settings.Commands;
using Inventory_Management.Application.Features.Settings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Settings")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SettingsController : ControllerBase
{
    private readonly ISender _sender;

    public SettingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("pos-payment-verification")]
    [Authorize]
    [EndpointSummary("Retrieve POS Payment Verification setting")]
    [EndpointDescription("Fetches whether POS payment verification via Cheki.et is enabled for the current user's tenant organization.")]
    [ProducesResponseType(typeof(PosPaymentVerificationSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PosPaymentVerificationSettingDto>> GetPosPaymentVerification(CancellationToken cancellationToken = default)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return Unauthorized("User does not belong to a tenant.");
        }

        var setting = await _sender.Send(new GetPosPaymentVerificationSettingQuery(tenantId), cancellationToken);
        return Ok(setting);
    }

    [HttpPut("pos-payment-verification")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Update POS Payment Verification setting")]
    [EndpointDescription("Enables or disables POS payment verification via Cheki.et for the current user's tenant organization. Only Admin and Manager roles are authorized.")]
    [ProducesResponseType(typeof(PosPaymentVerificationSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PosPaymentVerificationSettingDto>> UpdatePosPaymentVerification(
        [FromBody] PosPaymentVerificationSettingDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return Unauthorized("User does not belong to a tenant.");
        }

        var updated = await _sender.Send(new UpdatePosPaymentVerificationSettingCommand(tenantId, dto.Enabled), cancellationToken);
        return Ok(updated);
    }
}
