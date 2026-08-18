using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICurrentTenant _currentTenant;

    public AuthController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ICurrentTenant currentTenant)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _currentTenant = currentTenant;
    }

    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role,
        Guid? TenantId = null);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            // Prevent account enumeration by returning a generic response
            return Ok(new
            {
                message = "Registration request received."
            });
        }

        var tenantId = request.TenantId ?? _currentTenant.TenantId;

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TenantId = tenantId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        // Ensure requested role exists
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }
        await _userManager.AddToRoleAsync(user, request.Role);

        return Ok(new { message = "Registration successful.", tenantId });
    }

    public record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new
            {
                detail = "Invalid Credentials."
            });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return StatusCode(423, new
            {
                detail = "Account locked due to multiple failed login attempts. Try again in 15 minutes."
            });
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new
            {
                detail = "Invalid Credentials."
            });
        }

        // Reset failed attempt counter on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            tenantId = user.TenantId
        });
    }
}