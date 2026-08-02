using Inventory_Management.Application.DTOs.Purchase;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Purchases")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    [EndpointSummary("Retrieve all purchases")]
    [EndpointDescription("Fetches a complete list of all recorded purchase orders.")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetAll()
    {
        var purchases = await _purchaseService.GetAllAsync();
        return Ok(purchases);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Retrieve a purchase by ID")]
    [EndpointDescription("Fetches detailed information for a specific purchase order by ID.")]
    [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseDto>> GetById(Guid id)
    {
        var purchase = await _purchaseService.GetByIdAsync(id);
        return Ok(purchase);
    }

    [HttpPost]
    [EndpointSummary("Create a purchase order")]
    [EndpointDescription("Records a new purchase transaction with supplier and total amount.")]
    [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PurchaseDto>> Create([FromBody] CreatePurchaseDto dto)
    {
        var created = await _purchaseService.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a purchase order")]
    [EndpointDescription("Updates details of an existing purchase transaction.")]
    [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseDto>> Update(Guid id, [FromBody] UpdatePurchaseDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID in URL does not match ID in request body.");

        var updated = await _purchaseService.UpdateAsync(dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a purchase order")]
    [EndpointDescription("Removes a purchase order transaction by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _purchaseService.DeleteAsync(id);
        return NoContent();
    }
}
