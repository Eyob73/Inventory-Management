using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Inventory_Management.Infrastructure.Persistence.Data;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushSubscriptionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PushSubscriptionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.endpoint))
        {
            return BadRequest(new { message = "Invalid subscription data." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // If this device endpoint was previously associated with any other user on this device, remove it
        var staleSubscriptions = await _context.PushSubscriptions
            .Where(s => s.Endpoint == dto.endpoint && s.UserId != userId)
            .ToListAsync();

        if (staleSubscriptions.Count > 0)
        {
            _context.PushSubscriptions.RemoveRange(staleSubscriptions);
        }

        // Check if subscription already exists for this user and endpoint
        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.endpoint && s.UserId == userId);

        if (existing == null)
        {
            var subscription = new PushSubscription
            {
                UserId = userId,
                Endpoint = dto.endpoint,
                P256dh = dto.keys?.p256dh ?? string.Empty,
                Auth = dto.keys?.auth ?? string.Empty
            };
            _context.PushSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update keys if they changed
            existing.P256dh = dto.keys?.p256dh ?? existing.P256dh;
            existing.Auth = dto.keys?.auth ?? existing.Auth;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Subscribed to push notifications successfully." });
    }

    [HttpPost("unsubscribe")]
    [HttpDelete("unsubscribe")]
    [AllowAnonymous]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscriptionDto? dto = null, [FromQuery] string? endpoint = null)
    {
        var targetEndpoint = !string.IsNullOrWhiteSpace(dto?.endpoint) ? dto.endpoint : endpoint;
        if (string.IsNullOrWhiteSpace(targetEndpoint))
        {
            return BadRequest(new { message = "Endpoint is required." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // If authenticated, target records for this user;
        // if unauthenticated (e.g. called after signout/token revocation),
        // remove any subscription with this unique device endpoint so no pushes are sent to this device.
        var query = _context.PushSubscriptions.Where(s => s.Endpoint == targetEndpoint);
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(s => s.UserId == userId);
        }

        var existing = await query.ToListAsync();
        if (existing.Count > 0)
        {
            _context.PushSubscriptions.RemoveRange(existing);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Unsubscribed successfully." });
    }
}
