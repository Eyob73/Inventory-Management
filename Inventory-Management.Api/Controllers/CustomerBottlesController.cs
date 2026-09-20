using Inventory_Management.Application.Features.CustomerBottles.DTOs;
using Inventory_Management.Application.Features.CustomerBottles.Queries;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomerBottlesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerBottlesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerBottleBalanceDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetCustomerBottleBalancesQuery()));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<CustomerBottleBalanceDto>>> GetPaged([FromQuery] PagedRequest request)
    {
        return Ok(await _mediator.Send(new GetPagedCustomerBottlesQuery(request)));
    }

    [HttpGet("{customerId}")]
    public async Task<ActionResult<List<CustomerBottleBalanceDto>>> GetByCustomer(Guid customerId)
    {
        return Ok(await _mediator.Send(new GetCustomerBottleBalancesQuery { CustomerId = customerId }));
    }

    [HttpPost("return")]
    public async Task<ActionResult> ReturnBottle([FromBody] Inventory_Management.Application.Features.CustomerBottles.Commands.ReturnBottleCommand command)
    {
        command.User = User.Identity?.Name ?? "System";
        await _mediator.Send(command);
        return Ok();
    }
}
