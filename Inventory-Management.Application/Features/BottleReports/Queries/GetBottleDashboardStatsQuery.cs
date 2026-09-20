using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.BottleReports.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleReports.Queries;

public class GetBottleDashboardStatsQuery : IRequest<BottleDashboardStatsDto>
{
}

public class GetBottleDashboardStatsQueryHandler : IRequestHandler<GetBottleDashboardStatsQuery, BottleDashboardStatsDto>
{
        private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _invRepo;
    private readonly IGenericRepository<CustomerBottleBalance> _balRepo;
    private readonly IGenericRepository<BottleType> _btRepo;
    private readonly IGenericRepository<Product> _prodRepo;

    public GetBottleDashboardStatsQueryHandler(
        IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> invRepo, 
        IGenericRepository<CustomerBottleBalance> balRepo, 
        IGenericRepository<BottleType> btRepo,
        IGenericRepository<Product> prodRepo)
    {
        _invRepo = invRepo;
        _balRepo = balRepo;
        _btRepo = btRepo;
        _prodRepo = prodRepo;
    }

    public async Task<BottleDashboardStatsDto> Handle(GetBottleDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var inventory = await _invRepo.Query().AsNoTracking().ToListAsync(cancellationToken);
        var balances = await _balRepo.Query().AsNoTracking().ToListAsync(cancellationToken);
        var bottleTypes = await _btRepo.Query().CountAsync(cancellationToken);
        
        var products = await _prodRepo.Query()
            .Where(p => p.IsReturnable && p.BottleTypeId != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new BottleDashboardStatsDto
        {
            TotalBottleTypes = bottleTypes,
            TotalFullBottles = products.Sum(x => x.QuantityInStock),
            TotalEmptyBottles = inventory.Sum(x => x.EmptyBottles),
            TotalBottlesWithCustomers = inventory.Sum(x => x.WithCustomers),
            TotalDamagedBottles = inventory.Sum(x => x.DamagedBottles),
            TotalLostBottles = inventory.Sum(x => x.LostBottles),
            TotalPendingDepositLiability = balances.Sum(x => x.TotalDeposit)
        };
    }
}

