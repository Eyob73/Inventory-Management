using Inventory_Management.Application.Features.BottleReports.DTOs;
using Inventory_Management.Application.Features.BottleReports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BottleReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BottleReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<BottleReportResponse>> GetReport(
        [FromQuery] string type = "movement",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetBottleReportQuery
        {
            ReportType = type,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var result = await _mediator.Send(new GetBottleDashboardStatsQuery());
        return Ok(result);
    }
}
