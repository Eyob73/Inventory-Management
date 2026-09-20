using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.BottleTypes.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleTypes.Queries;

public class GetPagedBottleTypesQuery : IRequest<PagedResponse<BottleTypeDto>>
{
    public PagedRequest Request { get; set; }

    public GetPagedBottleTypesQuery(PagedRequest request)
    {
        Request = request;
    }
}

public class GetPagedBottleTypesQueryHandler : IRequestHandler<GetPagedBottleTypesQuery, PagedResponse<BottleTypeDto>>
{
    private readonly IGenericRepository<BottleType> _repository;

    public GetPagedBottleTypesQueryHandler(IGenericRepository<BottleType> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<BottleTypeDto>> Handle(GetPagedBottleTypesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Request.Search))
        {
            var search = request.Request.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search) || 
                                     (x.Description != null && x.Description.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = request.Request.Descending
            ? query.OrderByDescending(x => x.Name)
            : query.OrderBy(x => x.Name);

        var items = await query
            .Skip((request.Request.Page - 1) * request.Request.PageSize)
            .Take(request.Request.PageSize)
            .Select(x => new BottleTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                DepositAmount = x.DepositAmount,
                Capacity = x.Capacity,
                Material = x.Material,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                Quantity = x.BottleInventories.Any() ? x.BottleInventories.First().EmptyBottles : 0
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<BottleTypeDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Request.Page,
            PageSize = request.Request.PageSize
        };
    }
}
