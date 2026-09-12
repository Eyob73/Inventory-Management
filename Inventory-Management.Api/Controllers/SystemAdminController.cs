using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Features.SystemAdmin.Commands;
using Inventory_Management.Application.Features.SystemAdmin.Queries;
using Inventory_Management.Application.Features.Tenants.Queries;
using Inventory_Management.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/system")]
[Tags("SystemAdmin")]
[Authorize(Roles = "SystemAdmin")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SystemAdminController : ControllerBase
{
    private readonly ISender _sender;

    public SystemAdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<SystemDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        var dashboard = await _sender.Send(new GetSystemDashboardQuery(), cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAllTenants(CancellationToken cancellationToken = default)
    {
        var tenants = await _sender.Send(new GetAllTenantsQuery(), cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("tenants/paged")]
    public async Task<ActionResult<PagedResponse<TenantDto>>> GetPagedTenants([FromQuery] PagedRequest request, CancellationToken cancellationToken = default)
    {
        var tenants = await _sender.Send(new GetPagedTenantsQuery(request), cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("tenants/{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetTenantById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _sender.Send(new GetTenantByIdQuery(id), cancellationToken);
        return Ok(tenant);
    }

    [HttpPost("tenants")]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantWithAdminDto dto, CancellationToken cancellationToken = default)
    {
        var created = await _sender.Send(new CreateTenantWithAdminCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetTenantById), new { id = created.Id }, created);
    }

    [HttpPost("tenants/{id:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _sender.Send(new UpdateTenantStatusCommand(id, TenantStatus.Active), cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("tenants/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _sender.Send(new UpdateTenantStatusCommand(id, TenantStatus.Suspended), cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("tenants/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _sender.Send(new UpdateTenantStatusCommand(id, TenantStatus.Deactivated), cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpGet("tenants/{id:guid}/users")]
    public async Task<ActionResult<IEnumerable<TenantUserDto>>> GetTenantUsers(Guid id, CancellationToken cancellationToken = default)
    {
        var users = await _sender.Send(new GetTenantUsersQuery(id), cancellationToken);
        return Ok(users);
    }
}
