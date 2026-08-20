namespace Inventory_Management.Application.DTOs.Auth;

 public record RegisterRequestDto(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role
    );