using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.CustomerBottles.Commands;

public class ReturnBottleCommand : IRequest<bool>
{
    public Guid? CustomerId { get; set; }
    public Guid BottleTypeId { get; set; }
    public int Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public string User { get; set; } = string.Empty;
}

public class ReturnBottleCommandHandler : IRequestHandler<ReturnBottleCommand, bool>
{
    private readonly IGenericRepository<CustomerBottleBalance> _balRepo;
    private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _invRepo;
    private readonly IGenericRepository<BottleTransaction> _txRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnBottleCommandHandler(IGenericRepository<CustomerBottleBalance> balRepo, IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> invRepo, IGenericRepository<BottleTransaction> txRepo, IUnitOfWork unitOfWork)
    {
        _balRepo = balRepo;
        _invRepo = invRepo;
        _txRepo = txRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ReturnBottleCommand request, CancellationToken cancellationToken)
    {
        var balance = await _balRepo.Query()
            .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId && x.BottleTypeId == request.BottleTypeId, cancellationToken);
            
        if (balance == null || balance.Balance < request.Quantity)
        {
            throw new InvalidOperationException("Customer does not have enough bottles to return.");
        }

        balance.Balance -= request.Quantity;
        balance.TotalDeposit -= request.RefundAmount;
        balance.LastUpdatedAt = DateTime.UtcNow;

        var inventory = await _invRepo.Query().FirstOrDefaultAsync(x => x.BottleTypeId == request.BottleTypeId, cancellationToken);
        if (inventory != null)
        {
            inventory.WithCustomers -= request.Quantity;
            inventory.EmptyBottles += request.Quantity;
            inventory.LastUpdatedAt = DateTime.UtcNow;
        }

        await _txRepo.AddAsync(new BottleTransaction
        {
            BottleTypeId = request.BottleTypeId,
            CustomerId = request.CustomerId,
            TransactionType = BottleTransactionType.Returned,
            Quantity = request.Quantity,
            DepositAmount = request.RefundAmount * -1, // Negative because refunding
            CreatedBy = request.User,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
