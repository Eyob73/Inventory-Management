using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.Products.Commands;

public record CreateProductCommand(CreateProductDto Dto) : IRequest<ProductDto>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<BottleType> _bottleTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;

    public CreateProductCommandHandler(
        IGenericRepository<Product> productRepository, 
        IGenericRepository<BottleType> bottleTypeRepository,
        IUnitOfWork unitOfWork, 
        IFileStorageService fileStorageService,
        INotificationService notificationService)
    {
        _productRepository = productRepository;
        _bottleTypeRepository = bottleTypeRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    public async Task<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var dto = command.Dto;
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(dto.SKU))
            throw new ArgumentException("SKU is required.");

        var sku = dto.SKU.Trim();
        var skuTaken = await _productRepository.Query().AnyAsync(p => p.SKU == sku, cancellationToken);
        if (skuTaken)
            throw new ArgumentException($"A product with SKU '{sku}' already exists.");

        string? barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        if (barcode != null)
        {
            var barcodeTaken = await _productRepository.Query().AnyAsync(p => p.Barcode == barcode, cancellationToken);
            if (barcodeTaken)
                throw new ArgumentException($"A product with barcode '{barcode}' already exists.");
        }

        string? imageUrl = null;
        if (dto.Image != null)
        {
            imageUrl = await _fileStorageService.SaveProductImageAsync(dto.Image);
        }

        decimal depositAmount = 0;
        if (dto.IsReturnable && dto.BottleTypeId.HasValue)
        {
            var bt = await _bottleTypeRepository.GetByIdAsync(dto.BottleTypeId.Value);
            if (bt != null) depositAmount = bt.DepositAmount;
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            SKU = sku,
            Barcode = barcode,
            Description = dto.Description ?? string.Empty,
            ImageUrl = imageUrl,
            Price = dto.Price,
            Cost = dto.Cost,
            QuantityInStock = 0,
            MinimumStock = Math.Max(0, dto.MinimumStock),
            IsActive = dto.IsActive,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId,
            IsReturnable = dto.IsReturnable,
            BottleTypeId = dto.IsReturnable ? dto.BottleTypeId : null,
            BottleDepositAmount = depositAmount,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.SendToRolesAsync(
            new[] { "Admin", "Manager" },
            new Inventory_Management.Application.DTOs.Notification.NotificationDto
            {
                Title = "New Product Added",
                Message = $"Product '{product.Name}' (SKU: {product.SKU}) has been added to the catalog.",
                Type = "info",
                Icon = "inventory_2",
                RelatedEntityId = product.Id,
                RelatedEntityType = "Product",
                Link = $"/products/{product.Id}"
            });

        return Map(product);
    }

    private static ProductDto Map(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        SKU = p.SKU,
        Barcode = p.Barcode,
        Description = p.Description,
        ImageUrl = p.ImageUrl,
        Price = p.Price,
        Cost = p.Cost,
        QuantityInStock = p.QuantityInStock,
        MinimumStock = p.MinimumStock,
        IsActive = p.IsActive,
        CategoryId = p.CategoryId,
        SupplierId = p.SupplierId,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        IsReturnable = p.IsReturnable,
        BottleTypeId = p.BottleTypeId,
        BottleDepositAmount = p.BottleDepositAmount
    };
}




