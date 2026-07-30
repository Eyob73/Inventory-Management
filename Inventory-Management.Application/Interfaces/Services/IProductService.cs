using Inventory_Management.Application.DTOs.Product;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(Guid id);
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto> AddAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(Guid id);
}
