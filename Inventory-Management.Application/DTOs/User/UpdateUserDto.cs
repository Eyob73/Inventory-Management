using System.ComponentModel.DataAnnotations;

namespace Inventory_Management.Application.DTOs.User;

public class UpdateUserDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Role { get; set; }

    public Guid? TenantId { get; set; }
}
