using System.Linq;
using Inventory_Management.Application.DTOs.Auth;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Infrastructure.Persistence.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext context,
        TokenService tokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<(bool Success, IEnumerable<string>? Errors, bool AlreadyExists)> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, null, true);
        }
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return (false, errors, false);
        }
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(request.Role));
        }
        await _userManager.AddToRoleAsync(user, request.Role);
        return (true, null, false);
    }

    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new InvalidOperationException("Invalid credentials.");
        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new InvalidOperationException("Account locked due to multiple failed login attempts.");
        }
        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);
            throw new InvalidOperationException("Invalid credentials.");
        }
        await _userManager.ResetAccessFailedCountAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateJwt(user, roles);
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return (accessToken, refreshToken.Token);
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (storedToken == null)
        {
            throw new InvalidOperationException("Invalid refresh token.");
        }
        if (storedToken.IsUsed)
        {
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId)
                .ToListAsync();
            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
            }
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Token theft detected. All user sessions revoked.");
        }
        if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Refresh token expired or revoked.");
        }
        storedToken.IsUsed = true;
        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();
        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        var roles = await _userManager.GetRolesAsync(user!);
        var newAccessToken = _tokenService.GenerateJwt(user!, roles);
        return (newAccessToken, newRefreshToken.Token);
    }
}
