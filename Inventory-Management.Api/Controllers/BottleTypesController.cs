using Inventory_Management.Application.Features.BottleTypes.Commands;
using Inventory_Management.Application.Features.BottleTypes.DTOs;
using Inventory_Management.Application.Features.BottleTypes.Queries;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory_Management.Api.Filters;

namespace Inventory_Management.Api.Controllers;

[Authorize]
[RequireBottleManagement]
[ApiController]
[Route("api/[controller]")]
public class BottleTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BottleTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<BottleTypeDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetBottleTypesQuery()));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<BottleTypeDto>>> GetPaged([FromQuery] PagedRequest request)
    {
        return Ok(await _mediator.Send(new GetPagedBottleTypesQuery(request)));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBottleTypeCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateBottleTypeCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteBottleTypeCommand { Id = id });
        if (!result) return NotFound();
        return NoContent();
    }
}
