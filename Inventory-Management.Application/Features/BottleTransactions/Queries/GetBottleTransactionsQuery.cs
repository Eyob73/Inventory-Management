using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.BottleTransactions.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleTransactions.Queries;

public class GetBottleTransactionsQuery : IRequest<List<BottleTransactionDto>>
{
}

public class GetBottleTransactionsQueryHandler : IRequestHandler<GetBottleTransactionsQuery, List<BottleTransactionDto>>
{
    private readonly IGenericRepository<BottleTransaction> _repository;

    public GetBottleTransactionsQueryHandler(IGenericRepository<BottleTransaction> repository)
    {
        _repository = repository;
    }

    public async Task<List<BottleTransactionDto>> Handle(GetBottleTransactionsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.Query()
            .Include(x => x.BottleType)
            .Include(x => x.Customer)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
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
                ReferenceType = x.ReferenceType,
                ReferenceId = x.ReferenceId,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy
            })
            .ToListAsync(cancellationToken);
    }
}
