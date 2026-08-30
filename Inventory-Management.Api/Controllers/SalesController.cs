using System.Security.Claims;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager,Sales")]
[Tags("Sales")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly UserManager<AppUser> _userManager;

    public SalesController(ISaleService saleService, UserManager<AppUser> userManager)
    {
        _saleService = saleService;
        _userManager = userManager;
    }

    [HttpPost]
    [EndpointSummary("Create a POS sales transaction")]
    [EndpointDescription("Validates stock availability, deducts inventory, and records a sales transaction.")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaleDto>> Create([FromBody] CreateSaleDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? cashierName = null;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    cashierName = !string.IsNullOrWhiteSpace(user.FirstName)
                        ? $"{user.FirstName} {user.LastName}".Trim()
                        : user.UserName;
                }
            }

            var created = await _saleService.CreateSaleAsync(dto, userId, cashierName);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpGet("paged")]
    [EndpointSummary("Retrieve paged sales history")]
    [EndpointDescription("Fetches a paged list of recorded sales with optional search, date, cashier, and status filters.")]
    [ProducesResponseType(typeof(PagedResponse<SaleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SaleDto>>> GetPaged([FromQuery] SaleFilterDto filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? (User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : "Sales");

        var pagedSales = await _saleService.GetPagedSalesAsync(filter, userId, role);
        return Ok(pagedSales);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Retrieve a sale by ID")]
    [EndpointDescription("Fetches detailed information for a specific sales transaction by ID.")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> GetById(Guid id)
    {
        try
        {
            var sale = await _saleService.GetByIdAsync(id);
            return Ok(sale);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Cancel a sale transaction")]
    [EndpointDescription("Cancels a completed sales transaction and safely restores product stock.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.IsInRole("Admin") ? "Admin" : "Manager";

            await _saleService.CancelSaleAsync(id, userId, role);
            return NoContent();
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
}
