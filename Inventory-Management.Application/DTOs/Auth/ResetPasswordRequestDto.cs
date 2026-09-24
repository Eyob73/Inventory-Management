using System.ComponentModel.DataAnnotations;

namespace Inventory_Management.Application.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
