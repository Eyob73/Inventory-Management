using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.Products.Commands;

public record UpdateProductCommand(UpdateProductDto Dto) : IRequest<ProductDto>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public UpdateProductCommandHandler(IGenericRepository<Product> productRepository, IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var dto = command.Dto;
        var product = await _productRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Product with ID {dto.Id} not found.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(dto.SKU))
            throw new ArgumentException("SKU is required.");

        var sku = dto.SKU.Trim();
        var skuTaken = await _productRepository.Query()
            .AnyAsync(p => p.Id != dto.Id && p.SKU == sku, cancellationToken);
        if (skuTaken)
            throw new ArgumentException($"A product with SKU '{sku}' already exists.");

        if (dto.Image != null)
        {
            product.ImageUrl = await _fileStorageService.SaveProductImageAsync(dto.Image, product.ImageUrl);
        }
        else if (dto.RemoveImage && !string.IsNullOrEmpty(product.ImageUrl))
        {
            _fileStorageService.DeleteProductImage(product.ImageUrl);
            product.ImageUrl = null;
        }

        product.Name = dto.Name;
        product.SKU = sku;
        product.Description = dto.Description ?? string.Empty;
        product.Price = dto.Price;
        product.Cost = dto.Cost;
        product.MinimumStock = Math.Max(0, dto.MinimumStock);
        product.IsActive = dto.IsActive;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            Cost = product.Cost,
            QuantityInStock = product.QuantityInStock,
            MinimumStock = product.MinimumStock,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            SupplierId = product.SupplierId,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
