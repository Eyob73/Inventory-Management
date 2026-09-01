namespace Inventory_Management.Application.DTOs.Category;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
