using Inventory_Management.Application.DTOs.PurchaseItem;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Purchase Items")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PurchaseItemsController : ControllerBase
{
    private readonly IPurchaseItemService _purchaseItemService;

    public PurchaseItemsController(IPurchaseItemService purchaseItemService)
    {
        _purchaseItemService = purchaseItemService;
    }

    [HttpGet]
    [EndpointSummary("Retrieve all purchase items")]
    [EndpointDescription("Fetches a complete list of all purchase line items across all purchase orders.")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseItemDto>>> GetAll()
    {
        var items = await _purchaseItemService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Retrieve a purchase item by ID")]
    [EndpointDescription("Fetches details of a specific purchase line item by its unique identifier.")]
    [ProducesResponseType(typeof(PurchaseItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseItemDto>> GetById(Guid id)
    {
        var item = await _purchaseItemService.GetByIdAsync(id);
        return Ok(item);
    }

    [HttpPost]
    [EndpointSummary("Create a purchase item")]
    [EndpointDescription("Adds a new line item to a purchase order.")]
    [ProducesResponseType(typeof(PurchaseItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PurchaseItemDto>> Create([FromBody] CreatePurchaseItemDto dto)
    {
        var created = await _purchaseItemService.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a purchase item")]
    [EndpointDescription("Updates quantity, unit cost, or product details of an existing purchase line item.")]
    [ProducesResponseType(typeof(PurchaseItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseItemDto>> Update(Guid id, [FromBody] UpdatePurchaseItemDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID in URL does not match ID in request body.");

        var updated = await _purchaseItemService.UpdateAsync(dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a purchase item")]
    [EndpointDescription("Removes a line item from a purchase order by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _purchaseItemService.DeleteAsync(id);
        return NoContent();
    }
}
