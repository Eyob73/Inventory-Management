using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Features.Tenants.Commands;
using Inventory_Management.Application.Features.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Tenants")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [EndpointSummary("Retrieve all tenants")]
    [EndpointDescription("Fetches a complete list of all registered SaaS customer organizations.")]
    [ProducesResponseType(typeof(IEnumerable<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var tenants = await _sender.Send(new GetAllTenantsQuery(), cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Retrieve a tenant by ID")]
    [EndpointDescription("Fetches detailed information for a specific SaaS tenant by its unique identifier.")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _sender.Send(new GetTenantByIdQuery(id), cancellationToken);
        return Ok(tenant);
    }

    [HttpPost]
    [EndpointSummary("Create a new tenant")]
    [EndpointDescription("Registers a new SaaS customer tenant organization in the system.")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantDto dto, CancellationToken cancellationToken = default)
    {
        var created = await _sender.Send(new CreateTenantCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
