using Inventory_Management.Application.Features.BottleTransactions.DTOs;
using Inventory_Management.Application.Features.BottleTransactions.Queries;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BottleTransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BottleTransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<BottleTransactionDto>>> GetAll()
    {
        return Ok(await _mediator.Send(new GetBottleTransactionsQuery()));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<BottleTransactionDto>>> GetPaged([FromQuery] PagedRequest request)
    {
        return Ok(await _mediator.Send(new GetPagedBottleTransactionsQuery(request)));
    }
}
