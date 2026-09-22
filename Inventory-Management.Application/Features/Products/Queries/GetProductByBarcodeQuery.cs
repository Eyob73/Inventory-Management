using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.Products.Queries;

public record GetProductByBarcodeQuery(string Barcode) : IRequest<ProductDto>;

public class GetProductByBarcodeQueryHandler : IRequestHandler<GetProductByBarcodeQuery, ProductDto>
{
    private readonly IGenericRepository<Product> _repository;

    public GetProductByBarcodeQueryHandler(IGenericRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<ProductDto> Handle(GetProductByBarcodeQuery request, CancellationToken cancellationToken)
    {
        var barcode = request.Barcode.Trim();
        var product = await _repository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with barcode {barcode} not found.");

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
