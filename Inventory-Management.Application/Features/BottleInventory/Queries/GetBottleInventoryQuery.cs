using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.BottleInventory.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleInventory.Queries;

public class GetBottleInventoryQuery : IRequest<List<BottleInventoryDto>>
{
}

public class GetBottleInventoryQueryHandler : IRequestHandler<GetBottleInventoryQuery, List<BottleInventoryDto>>
{
        private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _repository;
    private readonly IGenericRepository<Product> _prodRepo;

    public GetBottleInventoryQueryHandler(
        IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> repository,
        IGenericRepository<Product> prodRepo)
    {
        _repository = repository;
        _prodRepo = prodRepo;
    }

    public async Task<List<BottleInventoryDto>> Handle(GetBottleInventoryQuery request, CancellationToken cancellationToken)
    {
        var inventory = await _repository.Query()
            .Include(x => x.BottleType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var products = await _prodRepo.Query()
            .Where(p => p.IsReturnable && p.BottleTypeId != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return inventory.Select(x => new BottleInventoryDto
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
    }
}

