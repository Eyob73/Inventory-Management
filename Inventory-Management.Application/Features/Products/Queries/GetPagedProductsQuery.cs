using System.Linq.Expressions;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Services;
using Inventory_Management.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.Products.Queries;

public record GetPagedProductsQuery(PagedRequest Request, bool IncludeCost = true) : IRequest<PagedResponse<ProductDto>>;

public class GetPagedProductsQueryHandler : IRequestHandler<GetPagedProductsQuery, PagedResponse<ProductDto>>
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<PurchaseItem>? _purchaseItemRepository;

    public GetPagedProductsQueryHandler(
        IGenericRepository<Product> productRepository,
        IGenericRepository<PurchaseItem>? purchaseItemRepository = null)
    {
        _productRepository = productRepository;
        _purchaseItemRepository = purchaseItemRepository;
    }

    public async Task<PagedResponse<ProductDto>> Handle(GetPagedProductsQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;

        Expression<Func<Product, bool>>? predicate = null;
        var search = request.Search?.Trim().ToLower();
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var hasCategory = request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty;
        var catId = request.CategoryId ?? Guid.Empty;
        var filterLowStock = request.Status == 1;
        var filterOutOfStock = request.Status == 2;

        predicate = p =>
            (!hasSearch || p.Name.ToLower().Contains(search!) || p.SKU.ToLower().Contains(search!) || (p.Barcode != null && p.Barcode.ToLower().Contains(search!)) || (p.Description != null && p.Description.ToLower().Contains(search!)))
            && (!hasCategory || p.CategoryId == catId)
            && (!filterLowStock || (p.QuantityInStock > 0 && p.QuantityInStock <= 15))
            && (!filterOutOfStock || p.QuantityInStock <= 0);

        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = request.OrderBy switch
        {
            "sku" => q => request.Descending ? q.OrderByDescending(p => p.SKU) : q.OrderBy(p => p.SKU),
            "price" => q => request.Descending ? q.OrderByDescending(p => p.Price) : q.OrderBy(p => p.Price),
            "quantityInStock" => q => request.Descending ? q.OrderByDescending(p => p.QuantityInStock) : q.OrderBy(p => p.QuantityInStock),
            "createdAt" => q => request.Descending ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            _ => q => request.Descending ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name)
        };

        var (items, totalCount) = await _productRepository.GetPagedAsync(request.Page, request.PageSize, predicate, orderBy);

        var mappedItems = items.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Barcode = p.Barcode,
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            Price = p.Price,
            Cost = query.IncludeCost ? p.Cost : 0m,
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
        }).ToList();

        var stockSnapshot = await _productRepository.Query()
            .AsNoTracking()
            .Select(p => new { p.Id, p.Cost, p.Price, p.QuantityInStock })
            .ToListAsync(cancellationToken);

        var lowStockCount = stockSnapshot.Count(p => p.QuantityInStock > 0 && p.QuantityInStock <= 15);
        var outOfStockCount = stockSnapshot.Count(p => p.QuantityInStock <= 0);

        decimal totalInventoryValue;
        if (query.IncludeCost)
        {
            var purchaseUnitCosts = await InventoryValuationHelper.GetPurchaseUnitCostsAsync(_purchaseItemRepository, cancellationToken);
            totalInventoryValue = InventoryValuationHelper.CalculateCostInventoryValue(
                stockSnapshot.Select(p => (p.Id, p.Cost, p.QuantityInStock)),
                purchaseUnitCosts);
        }
        else
        {
            totalInventoryValue = InventoryValuationHelper.CalculateSellingInventoryValue(
                stockSnapshot.Select(p => (p.QuantityInStock, p.Price)));
        }

        return new PagedResponse<ProductDto>
        {
            Items = mappedItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            TotalInventoryValue = totalInventoryValue
        };
    }
}
