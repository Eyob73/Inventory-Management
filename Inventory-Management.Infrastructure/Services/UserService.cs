using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.User;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            userDtos.Add(await MapToDtoAsync(user));
        }

        return userDtos;
    }

    public async Task<PagedResponse<UserDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 50 ? 50 : pageSize;

        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(lower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(lower)) ||
                (u.Email != null && u.Email.ToLower().Contains(lower)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(lower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pagedUsers = await query
            .OrderBy(u => u.FirstName ?? u.UserName ?? u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>();
        foreach (var user in pagedUsers)
            dtos.Add(await MapToDtoAsync(user));

        return new PagedResponse<UserDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        return await MapToDtoAsync(user);
    }

    public async Task<(bool Success, UserDto? User, IEnumerable<string>? Errors)> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return (false, null, new[] { $"A user with email '{dto.Email}' already exists." });
        }

        var user = new AppUser
        {
            UserName = !string.IsNullOrWhiteSpace(dto.UserName) ? dto.UserName : dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            TenantId = dto.TenantId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return (false, null, result.Errors.Select(e => e.Description));
        }

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(dto.Role));
            }
            await _userManager.AddToRoleAsync(user, dto.Role);
        }

        var userDto = await MapToDtoAsync(user);
        return (true, userDto, null);
    }

    public async Task<(bool Success, UserDto? User, IEnumerable<string>? Errors)> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return (false, null, new[] { "User not found." });
        }

        user.Email = dto.Email;
        user.UserName = !string.IsNullOrWhiteSpace(dto.UserName) ? dto.UserName : dto.Email;
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        if (dto.TenantId.HasValue)
        {
            user.TenantId = dto.TenantId;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, null, result.Errors.Select(e => e.Description));
        }

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(dto.Role));
            }
            await _userManager.AddToRoleAsync(user, dto.Role);
        }

        var userDto = await MapToDtoAsync(user);
        return (true, userDto, null);
    }

    public async Task<(bool Success, IEnumerable<string>? Errors)> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return (false, new[] { "User not found." });
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        return (true, null);
    }

    public async Task<(bool Success, bool IsLockedOut, IEnumerable<string>? Errors)> ToggleLockoutAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return (false, false, new[] { "User not found." });
        }

        var isCurrentlyLocked = await _userManager.IsLockedOutAsync(user);
        IdentityResult result;

        if (isCurrentlyLocked)
        {
            result = await _userManager.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }

        if (!result.Succeeded)
        {
            return (false, isCurrentlyLocked, result.Errors.Select(e => e.Description));
        }

        return (true, !isCurrentlyLocked, null);
    }

    private async Task<UserDto> MapToDtoAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var isLockedOut = await _userManager.IsLockedOutAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            TenantId = user.TenantId,
            Roles = roles,
            IsLockedOut = isLockedOut
        };
    }
}
