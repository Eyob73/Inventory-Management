using Inventory_Management.Application.Features.BottleReports.DTOs;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.BottleReports.Queries;

public class GetBottleReportQuery : IRequest<BottleReportResponse>
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GetBottleReportQueryHandler : IRequestHandler<GetBottleReportQuery, BottleReportResponse>
{
    private readonly IGenericRepository<BottleTransaction> _txRepo;
    private readonly IGenericRepository<CustomerBottleBalance> _balRepo;

    public GetBottleReportQueryHandler(
        IGenericRepository<BottleTransaction> txRepo,
        IGenericRepository<CustomerBottleBalance> balRepo)
    {
        _txRepo = txRepo;
        _balRepo = balRepo;
    }

    public async Task<BottleReportResponse> Handle(GetBottleReportQuery request, CancellationToken cancellationToken)
    {
        var response = new BottleReportResponse { ReportType = request.ReportType };
        
        // Base transaction query
        var txQuery = _txRepo.Query()
            .Include(x => x.BottleType)
            .Include(x => x.Customer)
            .AsNoTracking();

        if (request.StartDate.HasValue)
            txQuery = txQuery.Where(x => x.CreatedAt >= request.StartDate.Value);
        
        if (request.EndDate.HasValue)
        {
            var end = request.EndDate.Value.AddDays(1).AddTicks(-1);
            txQuery = txQuery.Where(x => x.CreatedAt <= end);
        }

        switch (request.ReportType.ToLower())
        {
            case "movement":
                var moves = await txQuery.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
                response.MovementData = moves.Select(x => new BottleMovementReportDto
                {
                    Date = x.CreatedAt,
                    TransactionType = x.TransactionType.ToString(),
                    BottleTypeName = x.BottleType?.Name ?? "",
                    CustomerName = x.Customer?.Name ?? (x.CustomerId == null && (x.TransactionType == BottleTransactionType.Issued || x.TransactionType == BottleTransactionType.Returned) ? "Walk-in / Anonymous" : ""),
                    Quantity = x.Quantity,
                    Reference = x.ReferenceType ?? ""
                }).ToList();
                break;

            case "deposit":
                var deposits = await txQuery.Where(x => x.DepositAmount > 0).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
                response.DepositData = deposits.Select(x => new BottleDepositReportDto
                {
                    Date = x.CreatedAt,
                    CustomerName = x.Customer?.Name ?? (x.CustomerId == null ? "Walk-in / Anonymous" : ""),
                    BottleTypeName = x.BottleType?.Name ?? "",
                    DepositAmount = x.DepositAmount,
                    TransactionType = x.TransactionType.ToString()
                }).ToList();
                break;

            case "customer":
                // Customer Balance doesn't typically filter by date as strictly, but we return current state
                var balances = await _balRepo.Query()
                    .Include(x => x.Customer)
                    .Include(x => x.BottleType)
                    .AsNoTracking()
                    .OrderBy(x => x.Customer != null ? x.Customer.Name : "Walk-in / Anonymous")
                    .ToListAsync(cancellationToken);
                    
                response.CustomerData = balances.Select(x => new CustomerBottleBalanceReportDto
                {
                    CustomerName = x.Customer?.Name ?? "Walk-in / Anonymous",
                    BottleTypeName = x.BottleType?.Name ?? "",
                    UnreturnedBottles = x.Balance,
                    TotalDeposit = x.TotalDeposit,
                    LastUpdatedAt = x.LastUpdatedAt
                }).ToList();
                break;

            case "loss":
                var losses = await txQuery
                    .Where(x => x.TransactionType == BottleTransactionType.Damaged || x.TransactionType == BottleTransactionType.Lost)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync(cancellationToken);
                    
                response.LossData = losses.Select(x => new BottleLossReportDto
                {
                    Date = x.CreatedAt,
                    BottleTypeName = x.BottleType?.Name ?? "",
                    TransactionType = x.TransactionType.ToString(),
                    Quantity = x.Quantity,
                    Notes = x.Notes ?? ""
                }).ToList();
                break;
        }

        return response;
    }
}
