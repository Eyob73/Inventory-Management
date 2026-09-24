using System.ComponentModel.DataAnnotations;

namespace Inventory_Management.Application.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
