using System.Security.Claims;
using Inventory_Management.Application.DTOs.Payment;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/payment-destinations")]
[Authorize(Roles = "Admin,Manager,Sales")]
[Tags("Payment Destinations")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PaymentDestinationsController : ControllerBase
{
    private readonly IPaymentDestinationService _destinationService;

    public PaymentDestinationsController(IPaymentDestinationService destinationService)
    {
        _destinationService = destinationService;
    }

    [HttpGet]
    [EndpointSummary("List payment destinations")]
    [EndpointDescription("Retrieves registered bank accounts and Telebirr receiving destinations for the current tenant.")]
    [ProducesResponseType(typeof(List<PaymentDestinationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentDestinationDto>>> GetAll([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var destinations = await _destinationService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(destinations);
    }

    [HttpGet("supported-banks")]
    [EndpointSummary("Discover supported banks")]
    [EndpointDescription("Retrieves the live list of supported banking institutions and mobile money providers supported by Cheki.")]
    [ProducesResponseType(typeof(List<ChekiBankDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChekiBankDto>>> GetSupportedBanks(CancellationToken cancellationToken)
    {
        var banks = await _destinationService.GetSupportedBanksAsync(cancellationToken);
        return Ok(banks);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get payment destination by ID")]
    [ProducesResponseType(typeof(PaymentDestinationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDestinationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var destination = await _destinationService.GetByIdAsync(id, cancellationToken);
            return Ok(destination);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Register a payment destination")]
    [ProducesResponseType(typeof(PaymentDestinationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentDestinationDto>> Create([FromBody] CreatePaymentDestinationDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var created = await _destinationService.CreateAsync(dto, userId, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Update a payment destination")]
    [ProducesResponseType(typeof(PaymentDestinationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDestinationDto>> Update(Guid id, [FromBody] UpdatePaymentDestinationDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _destinationService.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/default")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Set payment destination as default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _destinationService.SetDefaultAsync(id, cancellationToken);
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

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Activate or deactivate payment destination")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _destinationService.SetStatusAsync(id, request.IsActive, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [EndpointSummary("Delete a payment destination")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _destinationService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    public record SetStatusRequest(bool IsActive);
}
