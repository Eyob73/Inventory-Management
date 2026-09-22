using System.ComponentModel.DataAnnotations;

namespace Inventory_Management.Application.DTOs.User;

public class UpdateUserPreferencesDto
{
    [Required]
    [RegularExpression("^(en|am|om)$", ErrorMessage = "Language must be en, am, or om")]
    public string PreferredLanguage { get; set; } = "en";
}
