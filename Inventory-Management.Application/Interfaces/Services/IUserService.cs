using Inventory_Management.Application.DTOs.User;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<(bool Success, UserDto? User, IEnumerable<string>? Errors)> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, UserDto? User, IEnumerable<string>? Errors)> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, IEnumerable<string>? Errors)> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<(bool Success, bool IsLockedOut, IEnumerable<string>? Errors)> ToggleLockoutAsync(string id, CancellationToken cancellationToken = default);
}
