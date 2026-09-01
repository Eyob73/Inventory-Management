using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.Products.Commands;

public record CreateProductCommand(CreateProductDto Dto) : IRequest<ProductDto>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IGenericRepository<Product> productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var dto = command.Dto;
        if (string.IsNullOrWhiteSpace(dto.SKU))
            throw new ArgumentException("SKU is required.");

        var sku = dto.SKU.Trim();
        var skuTaken = await _productRepository.Query().AnyAsync(p => p.SKU == sku, cancellationToken);
        if (skuTaken)
            throw new ArgumentException($"A product with SKU '{sku}' already exists.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            SKU = sku,
            Description = dto.Description,
            Price = dto.Price,
            Cost = dto.Cost,
            QuantityInStock = 0,
            MinimumStock = Math.Max(0, dto.MinimumStock),
            IsActive = dto.IsActive,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(product);
    }

    private static ProductDto Map(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        SKU = p.SKU,
        Description = p.Description,
        Price = p.Price,
        Cost = p.Cost,
        QuantityInStock = p.QuantityInStock,
        MinimumStock = p.MinimumStock,
        IsActive = p.IsActive,
        CategoryId = p.CategoryId,
        SupplierId = p.SupplierId,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
