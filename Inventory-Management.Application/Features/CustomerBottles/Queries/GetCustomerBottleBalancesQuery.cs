using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.CustomerBottles.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.CustomerBottles.Queries;

public class GetCustomerBottleBalancesQuery : IRequest<List<CustomerBottleBalanceDto>>
{
    public Guid? CustomerId { get; set; }
}

public class GetCustomerBottleBalancesQueryHandler : IRequestHandler<GetCustomerBottleBalancesQuery, List<CustomerBottleBalanceDto>>
{
    private readonly IGenericRepository<CustomerBottleBalance> _repository;

    public GetCustomerBottleBalancesQueryHandler(IGenericRepository<CustomerBottleBalance> repository)
    {
        _repository = repository;
    }

    public async Task<List<CustomerBottleBalanceDto>> Handle(GetCustomerBottleBalancesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.Query()
            .Include(x => x.Customer)
            .Include(x => x.BottleType)
            .AsNoTracking();

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        return await query.Select(x => new CustomerBottleBalanceDto
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            CustomerName = x.Customer != null ? x.Customer.Name : "Walk-in / Anonymous",
            BottleTypeId = x.BottleTypeId,
            BottleTypeName = x.BottleType!.Name,
            Balance = x.Balance,
            TotalDeposit = x.TotalDeposit,
            LastUpdatedAt = x.LastUpdatedAt
        }).ToListAsync(cancellationToken);
    }
}
