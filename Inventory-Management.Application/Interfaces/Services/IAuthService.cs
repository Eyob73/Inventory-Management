using Inventory_Management.Application.DTOs.Auth;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, IEnumerable<string>? Errors, bool AlreadyExists)> RegisterAsync(RegisterRequestDto request);
    Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginRequestDto request);
    Task<(string AccessToken, string RefreshToken)> RefreshAsync(string refreshToken);
}
