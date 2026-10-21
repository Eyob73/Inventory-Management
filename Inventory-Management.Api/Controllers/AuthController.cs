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

            SetRefreshTokenCookie(refreshToken);

            return Ok(new { message = "Login successful.", accessToken, refreshToken });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("locked"))
        {
            return StatusCode(423, new { detail = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("suspended") || ex.Message.Contains("deactivated"))
        {
            return StatusCode(403, new { detail = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { detail = "Invalid credentials." });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto? request = null)
    {
        var refreshToken = string.IsNullOrEmpty(request?.RefreshToken) ? Request.Cookies["ims_refresh"] : request.RefreshToken;
        await _authService.LogoutAsync(refreshToken);

        DeleteRefreshTokenCookie();

        return Ok(new { message = "Logout successful." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
    {
        try
        {
            var tokenToRefresh = string.IsNullOrEmpty(request?.RefreshToken) ? Request.Cookies["ims_refresh"] : request.RefreshToken;
            if (string.IsNullOrEmpty(tokenToRefresh))
            {
                return Unauthorized(new { message = "Refresh token is missing." });
            }

            var (accessToken, refreshToken) = await _authService.RefreshAsync(tokenToRefresh);

            SetRefreshTokenCookie(refreshToken);

            return Ok(new { accessToken, refreshToken });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("theft"))
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("locked"))
        {
            return StatusCode(423, new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("suspended") || ex.Message.Contains("deactivated"))
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var isHttps = Request.IsHttps;
        Response.Cookies.Append("ims_refresh", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps || !_env.IsDevelopment(),
            SameSite = (isHttps || !_env.IsDevelopment()) ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        var isHttps = Request.IsHttps;
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps || !_env.IsDevelopment(),
            SameSite = (isHttps || !_env.IsDevelopment()) ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Delete("ims_refresh", options);
        Response.Cookies.Delete("ims_auth", options);
        Response.Cookies.Delete("XSRF-TOKEN", options);
    }

    [HttpPost("change-password")]
    [Authorize]
    [EndpointSummary("Change user password")]
    [EndpointDescription("Allows the user to update their account password.")]
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

    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var origin = Request.Headers["Origin"].FirstOrDefault();
        if (string.IsNullOrEmpty(origin))
        {
            var allowedOrigins = _env.IsDevelopment() ? new[] { "http://localhost:4200" } : new[] { "https://your-frontend-domain.com" }; // Usually pulled from config
            origin = allowedOrigins[0];
        }

        await _authService.ForgotPasswordAsync(request, origin);
        
        // Return a generic success response to prevent account enumeration
        return Ok(new { message = "If an account exists for this email address, you will receive a password reset link shortly." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var (success, errors) = await _authService.ResetPasswordAsync(request);
        if (!success)
        {
            // For security, don't indicate if the token is invalid or expired vs wrong email.
            // But we can return a generic 400 with the errors.
            return BadRequest(new { message = "This password reset link is invalid or has expired.", errors });
        }

        return Ok(new { message = "Password reset successfully." });
    }
}