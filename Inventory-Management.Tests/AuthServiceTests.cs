using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Inventory_Management.Infrastructure.Persistence.Data;
using Inventory_Management.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Inventory_Management.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly TokenService _tokenService;
    private readonly AuthService _authService;

    private readonly AppUser _testUser;
    private readonly Tenant _testTenant;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "SuperSecretKeyForTestingAuthTokenPersistence12345!" },
                { "Jwt:Issuer", "InventoryApi" },
                { "Jwt:Audience", "InventoryClients" },
                { "Jwt:ExpiryMinutes", "15" }
            })
            .Build();
        _tokenService = new TokenService(config);

        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        _emailServiceMock = new Mock<IEmailService>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _context,
            _tokenService,
            _emailServiceMock.Object);

        _testTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            IsActive = true,
            Status = TenantStatus.Active
        };
        _context.Tenants.Add(_testTenant);

        _testUser = new AppUser
        {
            Id = "user-123",
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            TenantId = _testTenant.Id
        };

        _userManagerMock.Setup(m => m.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(false);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(new List<string> { "Staff" });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task RefreshAsync_ValidActiveToken_RotatesTokenAndSetsGracePeriod()
    {
        var activeToken = new RefreshToken
        {
            Token = "active-token-1",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.Add(activeToken);
        await _context.SaveChangesAsync();

        var result = await _authService.RefreshAsync("active-token-1");

        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEqual("active-token-1", result.RefreshToken);

        var previousToken = await _context.RefreshTokens.FirstAsync(rt => rt.Token == "active-token-1");
        Assert.True(previousToken.IsUsed);
        // ExpiresAt was set to 60s in the future as the grace period
        Assert.True(previousToken.ExpiresAt > DateTime.UtcNow);
        Assert.True(previousToken.ExpiresAt <= DateTime.UtcNow.AddSeconds(65));

        var newToken = await _context.RefreshTokens.FirstAsync(rt => rt.Token == result.RefreshToken);
        Assert.False(newToken.IsUsed);
        Assert.False(newToken.IsRevoked);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentRequestWithinGracePeriod_ReturnsLatestActiveTokenWithoutTheftDetection()
    {
        // First token rotated
        var rotatedToken = new RefreshToken
        {
            Token = "rotated-token-1",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddSeconds(55), // within 60s grace period
            IsUsed = true,
            IsRevoked = false
        };
        var latestActiveToken = new RefreshToken
        {
            Token = "latest-active-token",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.AddRange(rotatedToken, latestActiveToken);
        await _context.SaveChangesAsync();

        // A concurrent API call sending the already-rotated token within grace period
        var result = await _authService.RefreshAsync("rotated-token-1");

        Assert.NotNull(result.AccessToken);
        Assert.Equal("latest-active-token", result.RefreshToken);

        // Ensure session was NOT revoked
        var activeInDb = await _context.RefreshTokens.FirstAsync(rt => rt.Token == "latest-active-token");
        Assert.False(activeInDb.IsRevoked);
    }

    [Fact]
    public async Task RefreshAsync_ReusedTokenAfterGracePeriod_DetectsTheftAndRevokesAllSessions()
    {
        var expiredUsedToken = new RefreshToken
        {
            Token = "stolen-token",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddSeconds(-10), // Grace period passed
            IsUsed = true,
            IsRevoked = false
        };
        var otherActiveToken = new RefreshToken
        {
            Token = "legitimate-token",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.AddRange(expiredUsedToken, otherActiveToken);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RefreshAsync("stolen-token"));

        Assert.Contains("Token theft detected", ex.Message);

        var legitInDb = await _context.RefreshTokens.FirstAsync(rt => rt.Token == "legitimate-token");
        Assert.True(legitInDb.IsRevoked);
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ThrowsException()
    {
        var revokedToken = new RefreshToken
        {
            Token = "revoked-token",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = true
        };
        _context.RefreshTokens.Add(revokedToken);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RefreshAsync("revoked-token"));

        Assert.Equal("Refresh token expired or revoked.", ex.Message);
    }

    [Fact]
    public async Task RefreshAsync_SuspendedTenant_ThrowsException()
    {
        _testTenant.Status = TenantStatus.Suspended;
        _context.Tenants.Update(_testTenant);

        var token = new RefreshToken
        {
            Token = "tenant-suspended-token",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RefreshAsync("tenant-suspended-token"));

        Assert.Contains("suspended", ex.Message);
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesTokenImmediately()
    {
        var token = new RefreshToken
        {
            Token = "token-to-logout",
            UserId = _testUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        await _authService.LogoutAsync("token-to-logout");

        var tokenInDb = await _context.RefreshTokens.FirstAsync(rt => rt.Token == "token-to-logout");
        Assert.True(tokenInDb.IsRevoked);
        Assert.True(tokenInDb.ExpiresAt < DateTime.UtcNow);
    }
}
