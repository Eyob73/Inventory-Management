using System.Linq.Expressions;
using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.Products.Queries;

public record GetPagedProductsQuery(PagedRequest Request) : IRequest<PagedResponse<ProductDto>>;

public class GetPagedProductsQueryHandler : IRequestHandler<GetPagedProductsQuery, PagedResponse<ProductDto>>
{
    private readonly IGenericRepository<Product> _productRepository;

    public GetPagedProductsQueryHandler(IGenericRepository<Product> productRepository)
    {
        _productRepository = productRepository;
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
        }).ToList();

        return new PagedResponse<ProductDto>
        {
            Items = mappedItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
