using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Sales")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet]
    [EndpointSummary("Retrieve all sales")]
    [EndpointDescription("Fetches a complete list of all recorded sales transactions.")]
    [ProducesResponseType(typeof(IEnumerable<SaleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetAll()
    {
        var sales = await _saleService.GetAllAsync();
        return Ok(sales);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Retrieve a sale by ID")]
    [EndpointDescription("Fetches detailed information for a specific sales transaction by ID.")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> GetById(Guid id)
    {
        var sale = await _saleService.GetByIdAsync(id);
        return Ok(sale);
    }

    [HttpPost]
    [EndpointSummary("Create a sales transaction")]
    [EndpointDescription("Records a new sales transaction with customer and total amount.")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaleDto>> Create([FromBody] CreateSaleDto dto)
    {
        var created = await _saleService.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a sales transaction")]
    [EndpointDescription("Updates details of an existing sales transaction.")]
    [ProducesResponseType(typeof(SaleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> Update(Guid id, [FromBody] UpdateSaleDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID in URL does not match ID in request body.");

        var updated = await _saleService.UpdateAsync(dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a sales transaction")]
    [EndpointDescription("Removes a sales transaction by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _saleService.DeleteAsync(id);
        return NoContent();
    }
}
