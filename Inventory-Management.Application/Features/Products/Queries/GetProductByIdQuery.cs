using Inventory_Management.Application.DTOs.Product;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IGenericRepository<Product> _productRepository;

    public GetProductByIdQueryHandler(IGenericRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await _productRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Product with ID {request.Id} not found.");

        return new ProductDto
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
        };
    }
}
