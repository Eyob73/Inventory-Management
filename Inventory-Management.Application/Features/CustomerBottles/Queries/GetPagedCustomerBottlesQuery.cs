using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.CustomerBottles.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.CustomerBottles.Queries;

public class GetPagedCustomerBottlesQuery : IRequest<PagedResponse<CustomerBottleBalanceDto>>
{
    public PagedRequest Request { get; set; }

    public GetPagedCustomerBottlesQuery(PagedRequest request)
    {
        Request = request;
    }
}

public class GetPagedCustomerBottlesQueryHandler : IRequestHandler<GetPagedCustomerBottlesQuery, PagedResponse<CustomerBottleBalanceDto>>
{
    private readonly IGenericRepository<CustomerBottleBalance> _repository;

    public GetPagedCustomerBottlesQueryHandler(IGenericRepository<CustomerBottleBalance> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<CustomerBottleBalanceDto>> Handle(GetPagedCustomerBottlesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.Query()
            .Include(x => x.Customer)
            .Include(x => x.BottleType)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Request.Search))
        {
            var search = request.Request.Search.ToLower();
            query = query.Where(x => (x.Customer != null && x.Customer.Name.ToLower().Contains(search)) ||
                                     (x.BottleType != null && x.BottleType.Name.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.Request.Descending
            ? query.OrderByDescending(x => x.Customer != null ? x.Customer.Name : "Walk-in / Anonymous")
            : query.OrderBy(x => x.Customer != null ? x.Customer.Name : "Walk-in / Anonymous");

        var items = await query
            .Skip((request.Request.Page - 1) * request.Request.PageSize)
            .Take(request.Request.PageSize)
            .Select(x => new CustomerBottleBalanceDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : "Walk-in / Anonymous",
                BottleTypeId = x.BottleTypeId,
                BottleTypeName = x.BottleType!.Name,
                Balance = x.Balance,
                TotalDeposit = x.TotalDeposit,
                LastUpdatedAt = x.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<CustomerBottleBalanceDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Request.Page,
            PageSize = request.Request.PageSize
        };
    }
}
