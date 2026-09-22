using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.BottleInventory.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleInventory.Queries;

public class GetPagedBottleInventoryQuery : IRequest<PagedResponse<BottleInventoryDto>>
{
    public PagedRequest Request { get; set; }

    public GetPagedBottleInventoryQuery(PagedRequest request)
    {
        Request = request;
    }
}

public class GetPagedBottleInventoryQueryHandler : IRequestHandler<GetPagedBottleInventoryQuery, PagedResponse<BottleInventoryDto>>
{
    private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _repository;
    private readonly IGenericRepository<Product> _prodRepo;

    public GetPagedBottleInventoryQueryHandler(
        IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> repository,
        IGenericRepository<Product> prodRepo)
    {
        _repository = repository;
        _prodRepo = prodRepo;
    }

    public async Task<PagedResponse<BottleInventoryDto>> Handle(GetPagedBottleInventoryQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.Query().Include(x => x.BottleType).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Request.Search))
        {
            var search = request.Request.Search.ToLower();
            query = query.Where(x => x.BottleType != null && x.BottleType.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.Request.Descending
            ? query.OrderByDescending(x => x.BottleType!.Name)
            : query.OrderBy(x => x.BottleType!.Name);

        var inventoryPage = await query
            .Skip((request.Request.Page - 1) * request.Request.PageSize)
            .Take(request.Request.PageSize)
            .ToListAsync(cancellationToken);

        var products = await _prodRepo.Query()
            .Where(p => p.IsReturnable && p.BottleTypeId != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = inventoryPage.Select(x => new BottleInventoryDto
        {
            Id = x.Id,
            BottleTypeId = x.BottleTypeId,
            BottleTypeName = x.BottleType!.Name,
            FullBottles = products.Where(p => p.BottleTypeId == x.BottleTypeId).Sum(p => p.QuantityInStock),
            EmptyBottles = x.EmptyBottles,
            DamagedBottles = x.DamagedBottles,
            LostBottles = x.LostBottles,
            WithCustomers = x.WithCustomers,
            LastUpdatedAt = x.LastUpdatedAt
        }).ToList();

        return new PagedResponse<BottleInventoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Request.Page,
            PageSize = request.Request.PageSize
        };
    }
}
