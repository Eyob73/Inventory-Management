using Inventory_Management.Application.DTOs.Category;
using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Product> productRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDetailDto> GetByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Category with ID {id} not found.");

        var allProducts = await _productRepository.GetAllAsync();
        var categoryProducts = allProducts
            .Where(p => p.CategoryId == id && !p.IsDeleted)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Description = p.Description,
                Price = p.Price,
                Cost = p.Cost,
                QuantityInStock = p.QuantityInStock,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToList();

        return new CategoryDetailDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ProductCount = categoryProducts.Count,
            CreatedAt = category.CreatedAt,
            Products = categoryProducts
        };
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();

        var productCountMap = products
            .Where(p => !p.IsDeleted)
            .GroupBy(p => p.CategoryId)
            .ToDictionary(g => g.Key, g => g.Count());

        return categories.Select(c => MapToDto(c, productCountMap.GetValueOrDefault(c.Id, 0)));
    }

    public async Task<CategoryDto> AddAsync(CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Category name is required.");
        }

        var allCategories = await _categoryRepository.GetAllAsync();
        if (allCategories.Any(c => c.Name.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A category with the name '{dto.Name.Trim()}' already exists.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(category, 0);
    }

    public async Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Category name is required.");
        }

        var category = await _categoryRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Category with ID {dto.Id} not found.");

        var allCategories = await _categoryRepository.GetAllAsync();
        if (allCategories.Any(c => c.Id != dto.Id && c.Name.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Another category with the name '{dto.Name.Trim()}' already exists.");
        }

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim() ?? string.Empty;
        category.IsActive = dto.IsActive;

        await _categoryRepository.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        var products = await _productRepository.GetAllAsync();
        int count = products.Count(p => p.CategoryId == dto.Id && !p.IsDeleted);

        return MapToDto(category, count);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Category with ID {id} not found.");

        var products = await _productRepository.GetAllAsync();
        var hasProducts = products.Any(p => p.CategoryId == id && !p.IsDeleted);

        if (hasProducts)
        {
            throw new InvalidOperationException($"Cannot delete category '{category.Name}' because it has active products assigned to it.");
        }

        await _categoryRepository.DeleteAsync(category.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static CategoryDto MapToDto(Category c, int productCount) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        IsActive = c.IsActive,
        ProductCount = productCount,
        CreatedAt = c.CreatedAt
    };
}
