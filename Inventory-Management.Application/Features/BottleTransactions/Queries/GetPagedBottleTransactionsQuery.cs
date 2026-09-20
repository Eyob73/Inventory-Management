using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.BottleTransactions.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.DTOs.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleTransactions.Queries;

public class GetPagedBottleTransactionsQuery : IRequest<PagedResponse<BottleTransactionDto>>
{
    public PagedRequest Request { get; set; }

    public GetPagedBottleTransactionsQuery(PagedRequest request)
    {
        Request = request;
    }
}

public class GetPagedBottleTransactionsQueryHandler : IRequestHandler<GetPagedBottleTransactionsQuery, PagedResponse<BottleTransactionDto>>
{
    private readonly IGenericRepository<BottleTransaction> _repository;

    public GetPagedBottleTransactionsQueryHandler(IGenericRepository<BottleTransaction> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<BottleTransactionDto>> Handle(GetPagedBottleTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.Query()
            .Include(x => x.BottleType)
            .Include(x => x.Customer)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Request.Search))
        {
            var search = request.Request.Search.ToLower();
            query = query.Where(x => (x.BottleType != null && x.BottleType.Name.ToLower().Contains(search)) ||
                                     (x.Customer != null && x.Customer.Name.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.Request.Descending
            ? query.OrderByDescending(x => x.CreatedAt)
            : query.OrderBy(x => x.CreatedAt);

        var items = await query
            .Skip((request.Request.Page - 1) * request.Request.PageSize)
            .Take(request.Request.PageSize)
            .Select(x => new BottleTransactionDto
            {
                Id = x.Id,
                BottleTypeId = x.BottleTypeId,
                BottleTypeName = x.BottleType!.Name,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : null,
                TransactionType = x.TransactionType.ToString(),
                Quantity = x.Quantity,
                DepositAmount = x.DepositAmount,
                Notes = x.Notes,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<BottleTransactionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Request.Page,
            PageSize = request.Request.PageSize
        };
    }
}
