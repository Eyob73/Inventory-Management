using Inventory_Management.Application.Features.BottleInventory.Commands;
using Inventory_Management.Application.Features.BottleInventory.DTOs;
using Inventory_Management.Application.Features.BottleInventory.Queries;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BottleInventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public BottleInventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<BottleInventoryDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetBottleInventoryQuery()));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<BottleInventoryDto>>> GetPaged([FromQuery] PagedRequest request)
    {
        return Ok(await _mediator.Send(new GetPagedBottleInventoryQuery(request)));
    }

    [HttpPost("adjust")]
    public async Task<ActionResult> Adjust([FromBody] AdjustBottleInventoryCommand command)
    {
        // Add user info
        command.User = User.Identity?.Name ?? "System";
        var result = await _mediator.Send(command);
        if (!result) return NotFound();
        return Ok();
    }
}
