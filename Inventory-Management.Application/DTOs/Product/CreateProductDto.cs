using Microsoft.AspNetCore.Http;

namespace Inventory_Management.Application.DTOs.Product;

public class CreateProductDto
{
    public string? Name { get; set; }
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public IFormFile? Image { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int QuantityInStock { get; set; }
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
}
