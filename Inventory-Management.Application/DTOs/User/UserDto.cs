namespace Inventory_Management.Application.DTOs.User;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid? TenantId { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsLockedOut { get; set; }
}
