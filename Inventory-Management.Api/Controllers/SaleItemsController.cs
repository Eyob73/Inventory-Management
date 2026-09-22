using Inventory_Management.Application.DTOs.SaleItem;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Sale Items")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SaleItemsController : ControllerBase
{
    private readonly ISaleItemService _saleItemService;

    public SaleItemsController(ISaleItemService saleItemService)
    {
        _saleItemService = saleItemService;
    }

    [HttpGet]
    [EndpointSummary("Retrieve all sale items")]
    [EndpointDescription("Fetches a complete list of all sale line items across all sales transactions.")]
    [ProducesResponseType(typeof(IEnumerable<SaleItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SaleItemDto>>> GetAll()
    {
        var items = await _saleItemService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Retrieve a sale item by ID")]
    [EndpointDescription("Fetches details of a specific sale line item by ID.")]
    [ProducesResponseType(typeof(SaleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleItemDto>> GetById(Guid id)
    {
        var item = await _saleItemService.GetByIdAsync(id);
        return Ok(item);
    }

    [HttpPost]
    [EndpointSummary("Create a sale item")]
    [EndpointDescription("Adds a new line item to a sales transaction.")]
    [ProducesResponseType(typeof(SaleItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaleItemDto>> Create([FromBody] CreateSaleItemDto dto)
    {
        var created = await _saleItemService.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a sale item")]
    [EndpointDescription("Updates quantity, unit price, or product details of an existing sale line item.")]
    [ProducesResponseType(typeof(SaleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleItemDto>> Update(Guid id, [FromBody] UpdateSaleItemDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID in URL does not match ID in request body.");

        var updated = await _saleItemService.UpdateAsync(dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a sale item")]
    [EndpointDescription("Removes a line item from a sales transaction by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _saleItemService.DeleteAsync(id);
        return NoContent();
    }
}
