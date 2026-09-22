using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleInventory.Commands;

public enum BottleAdjustmentType
{
    Damaged,
    Lost,
    Found,
    Correction
}

public class AdjustBottleInventoryCommand : IRequest<bool>
{
    public Guid BottleTypeId { get; set; }
    public BottleAdjustmentType AdjustmentType { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}

public class AdjustBottleInventoryCommandHandler : IRequestHandler<AdjustBottleInventoryCommand, bool>
{
    private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _invRepo;
    private readonly IGenericRepository<BottleTransaction> _txRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustBottleInventoryCommandHandler(IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> invRepo, IGenericRepository<BottleTransaction> txRepo, IUnitOfWork unitOfWork)
    {
        _invRepo = invRepo;
        _txRepo = txRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(AdjustBottleInventoryCommand request, CancellationToken cancellationToken)
    {
        var inventory = await _invRepo.Query().FirstOrDefaultAsync(x => x.BottleTypeId == request.BottleTypeId, cancellationToken);
        if (inventory == null) return false;

        var transactionType = BottleTransactionType.Adjustment;
        if (request.AdjustmentType == BottleAdjustmentType.Damaged)
        {
            inventory.EmptyBottles -= request.Quantity;
            inventory.DamagedBottles += request.Quantity;
            transactionType = BottleTransactionType.Damaged;
        }
        else if (request.AdjustmentType == BottleAdjustmentType.Lost)
        {
            inventory.EmptyBottles -= request.Quantity;
            inventory.LostBottles += request.Quantity;
            transactionType = BottleTransactionType.Lost;
        }
        else if (request.AdjustmentType == BottleAdjustmentType.Found)
        {
            inventory.EmptyBottles += request.Quantity;
        }
        else if (request.AdjustmentType == BottleAdjustmentType.Correction)
        {
            inventory.EmptyBottles += request.Quantity; // Can be negative
        }

        inventory.LastUpdatedAt = DateTime.UtcNow;

        var transaction = new BottleTransaction
        {
            BottleTypeId = request.BottleTypeId,
            TransactionType = transactionType,
            Quantity = request.Quantity,
            Notes = $"[{request.AdjustmentType}] {request.Reason}",
            CreatedBy = request.User,
            CreatedAt = DateTime.UtcNow
        };

        await _txRepo.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
