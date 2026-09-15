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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Check if it already exists
        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.endpoint && s.UserId == userId);

        if (existing == null)
        {
            var subscription = new PushSubscription
            {
                UserId = userId,
                Endpoint = dto.endpoint,
                P256dh = dto.keys.p256dh,
                Auth = dto.keys.auth
            };
            _context.PushSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Subscribed to push notifications successfully." });
    }

    [HttpDelete("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscriptionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.endpoint && s.UserId == userId);

        if (existing != null)
        {
            _context.PushSubscriptions.Remove(existing);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Unsubscribed successfully." });
    }
}
