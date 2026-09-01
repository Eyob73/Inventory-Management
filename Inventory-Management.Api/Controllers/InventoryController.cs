using System.Security.Claims;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Inventory;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[Authorize(Roles = "Admin,Manager")]
[ApiController]
[Route("api/inventory")]
[Tags("Inventory")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("transactions")]
    [EndpointSummary("Retrieve inventory transactions")]
    [ProducesResponseType(typeof(PagedResponse<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<InventoryTransactionDto>>> GetTransactions(
        [FromQuery] InventoryTransactionFilterDto filter)
    {
        var result = await _inventoryService.GetTransactionsAsync(filter);
        return Ok(result);
    }

    [HttpGet("products/{productId:guid}")]
    [EndpointSummary("Retrieve current stock and recent movements for a product")]
    [ProducesResponseType(typeof(ProductStockDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductStockDto>> GetProductStock(Guid productId)
    {
        try
        {
            var stock = await _inventoryService.GetProductStockAsync(productId);
            return Ok(stock);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("adjustments")]
    [EndpointSummary("Create a stock adjustment")]
    [ProducesResponseType(typeof(ProductStockDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductStockDto>> Adjust([FromBody] CreateStockAdjustmentDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _inventoryService.AdjustStockAsync(dto.ProductId, dto.Quantity, dto.Notes, userId);
            var stock = await _inventoryService.GetProductStockAsync(dto.ProductId);
            return Ok(stock);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
