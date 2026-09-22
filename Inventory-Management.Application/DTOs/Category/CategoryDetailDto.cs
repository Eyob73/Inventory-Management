using Inventory_Management.Application.DTOs.Product;

namespace Inventory_Management.Application.DTOs.Category;

public class CategoryDetailDto : CategoryDto
{
    public List<ProductDto> Products { get; set; } = new();
}
