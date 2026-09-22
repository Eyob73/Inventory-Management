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
    private readonly IGenericRepository<BottleType> _bottleTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public UpdateProductCommandHandler(IGenericRepository<Product> productRepository, IGenericRepository<BottleType> bottleTypeRepository, IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _productRepository = productRepository;
        _bottleTypeRepository = bottleTypeRepository;
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

        string? barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        if (barcode != null)
        {
            var barcodeTaken = await _productRepository.Query()
                .AnyAsync(p => p.Id != dto.Id && p.Barcode == barcode, cancellationToken);
            if (barcodeTaken)
                throw new ArgumentException($"A product with barcode '{barcode}' already exists.");
        }

        if (dto.Image != null)
        {
            product.ImageUrl = await _fileStorageService.SaveProductImageAsync(dto.Image, product.ImageUrl);
        }
        else if (dto.RemoveImage && !string.IsNullOrEmpty(product.ImageUrl))
        {
            _fileStorageService.DeleteProductImage(product.ImageUrl);
            product.ImageUrl = null;
        }

                decimal depositAmount = 0;
        if (dto.IsReturnable && dto.BottleTypeId.HasValue)
        {
            var bt = await _bottleTypeRepository.GetByIdAsync(dto.BottleTypeId.Value);
            if (bt != null) depositAmount = bt.DepositAmount;
        }

        product.Name = dto.Name;
        product.SKU = sku;
        product.Barcode = barcode;
        product.Description = dto.Description ?? string.Empty;
        product.Price = dto.Price;
        product.Cost = dto.Cost;
        product.MinimumStock = Math.Max(0, dto.MinimumStock);
        product.IsActive = dto.IsActive;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;
        product.IsReturnable = dto.IsReturnable;
        product.BottleTypeId = dto.IsReturnable ? dto.BottleTypeId : null;
        product.BottleDepositAmount = depositAmount;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Barcode = product.Barcode,
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
            UpdatedAt = product.UpdatedAt,
            IsReturnable = product.IsReturnable,
            BottleTypeId = product.BottleTypeId,
            BottleDepositAmount = product.BottleDepositAmount
        };
    }
}



