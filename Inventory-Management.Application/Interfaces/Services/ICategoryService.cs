using Inventory_Management.Application.DTOs.Category;

namespace Inventory_Management.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<CategoryDto> GetByIdAsync(Guid id);
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto> AddAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto);
    Task DeleteAsync(Guid id);
}
