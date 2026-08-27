using System.ComponentModel.DataAnnotations;

namespace Inventory_Management.Application.DTOs.User;

public class CreateUserDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = "User";

    public Guid? TenantId { get; set; }
}
