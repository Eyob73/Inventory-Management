using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.BottleTypes.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleTypes.Queries;

public class GetBottleTypesQuery : IRequest<List<BottleTypeDto>>
{
}

public class GetBottleTypesQueryHandler : IRequestHandler<GetBottleTypesQuery, List<BottleTypeDto>>
{
    private readonly IGenericRepository<BottleType> _repository;

    public GetBottleTypesQueryHandler(IGenericRepository<BottleType> repository)
    {
        _repository = repository;
    }

    public async Task<List<BottleTypeDto>> Handle(GetBottleTypesQuery request, CancellationToken cancellationToken)
    {
        return await _repository.Query()
            .AsNoTracking()
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
    }
}

