using System.Security.Claims;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Inventory_Management.Application.DTOs.Auth;
using Microsoft.AspNetCore.RateLimiting;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var (success, errors, alreadyExists) = await _authService.RegisterAsync(request);
        if (alreadyExists)
        {
            return Ok(new { message = "Registration request received." });
        }
        if (!success)
        {
            return BadRequest(new { errors });
        }
        return Ok(new { message = "Registration successful." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var (accessToken, refreshToken) = await _authService.LoginAsync(request);

            Response.Cookies.Append("ims_auth", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("ims_refresh", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new { message = "Login successful." });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("locked"))
        {
            return StatusCode(423, new { detail = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { detail = "Invalid credentials." });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["ims_refresh"];
        await _authService.LogoutAsync(refreshToken);

        Response.Cookies.Delete("ims_auth");
        Response.Cookies.Delete("ims_refresh");

        return Ok(new { message = "Logout successful." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me([FromServices] UserManager<AppUser> userManager)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { detail = "User not found." });

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            userName = user.UserName,
            firstName = user.FirstName,
            lastName = user.LastName,
            roles = roles
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
    {
        try
        {
            var (accessToken, refreshToken) = await _authService.RefreshAsync(request.RefreshToken);
            return Ok(new { accessToken, refreshToken });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("theft"))
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    [EndpointSummary("Change user password")]
    [EndpointDescription("Allows the currently authenticated user to update their account password.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto,
        [FromServices] UserManager<AppUser> userManager)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { detail = "User not found." });

        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        return Ok(new { message = "Password updated successfully." });
    }
}