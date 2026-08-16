namespace Inventory_Management.Application.DTOs.Product;

public class UpdateProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int QuantityInStock { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
}
