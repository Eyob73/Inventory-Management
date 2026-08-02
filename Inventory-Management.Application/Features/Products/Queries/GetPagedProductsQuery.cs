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
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            predicate = p => p.Name.ToLower().Contains(search)
                          || p.SKU.ToLower().Contains(search)
                          || (p.Description != null && p.Description.ToLower().Contains(search));
        }

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
            Description = p.Description,
            Price = p.Price,
            Cost = p.Cost,
            QuantityInStock = p.QuantityInStock,
            CategoryId = p.CategoryId,
            SupplierId = p.SupplierId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
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
