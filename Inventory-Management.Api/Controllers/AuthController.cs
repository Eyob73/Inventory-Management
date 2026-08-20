using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Inventory_Management.Application.DTOs.Auth;

namespace Inventory_Management.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var (accessToken, refreshToken) = await _authService.LoginAsync(request);
            return Ok(new { accessToken, refreshToken });
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
            return Unauthorized(new { detail = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { detail = "Refresh token expired or revoked." });
        }
    }
}