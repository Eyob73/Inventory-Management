using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.Sales.Commands;

public class ProcessSaleBottleItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public bool IsBottleExchange { get; set; }
    public decimal BottleDepositAmount { get; set; }
}

public class ProcessSaleBottlesCommand : IRequest<bool>
{
    public Guid SaleId { get; set; }
    public Guid? CustomerId { get; set; }
    public string User { get; set; } = string.Empty;
    public List<ProcessSaleBottleItem> Items { get; set; } = new();
}

public class ProcessSaleBottlesCommandHandler : IRequestHandler<ProcessSaleBottlesCommand, bool>
{
    private readonly IGenericRepository<Product> _prodRepo;
    private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _invRepo;
    private readonly IGenericRepository<BottleTransaction> _txRepo;
    private readonly IGenericRepository<CustomerBottleBalance> _balRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessSaleBottlesCommandHandler(IGenericRepository<Product> prodRepo, IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> invRepo, IGenericRepository<BottleTransaction> txRepo, IGenericRepository<CustomerBottleBalance> balRepo, IUnitOfWork unitOfWork)
    {
        _prodRepo = prodRepo;
        _invRepo = invRepo;
        _txRepo = txRepo;
        _balRepo = balRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ProcessSaleBottlesCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await _prodRepo.Query().Where(x => productIds.Contains(x.Id)).ToListAsync(cancellationToken);

        foreach (var item in request.Items)
        {
            var product = products.FirstOrDefault(x => x.Id == item.ProductId);
            if (product == null || !product.BottleTypeId.HasValue) continue;

            var bottleTypeId = product.BottleTypeId.Value;
            var inventory = await _invRepo.Query().FirstOrDefaultAsync(x => x.BottleTypeId == bottleTypeId, cancellationToken);
            if (inventory == null)
            {
                inventory = new Inventory_Management.Domain.Entities.BottleInventory { BottleTypeId = bottleTypeId };
                await _invRepo.AddAsync(inventory);
            }

            // Decrease full bottles for every sale of returnable drink
            inventory.FullBottles -= item.Quantity;
            
            if (item.IsBottleExchange)
            {
                // Customer provided empty bottles in exchange
                inventory.EmptyBottles += item.Quantity;
                
                await _txRepo.AddAsync(new BottleTransaction
                {
                    BottleTypeId = bottleTypeId,
                    CustomerId = request.CustomerId,
                    TransactionType = BottleTransactionType.Exchanged,
                    Quantity = item.Quantity,
                    ReferenceType = "Sale",
                    ReferenceId = request.SaleId,
                    CreatedBy = request.User,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Customer took bottles and paid deposit
                inventory.WithCustomers += item.Quantity;
                
                var balance = await _balRepo.Query().FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId && x.BottleTypeId == bottleTypeId, cancellationToken);
                if (balance == null)
                {
                    balance = new CustomerBottleBalance { CustomerId = request.CustomerId, BottleTypeId = bottleTypeId };
                    await _balRepo.AddAsync(balance);
                }
                balance.Balance += item.Quantity;
                balance.TotalDeposit += (item.Quantity * item.BottleDepositAmount);
                balance.LastUpdatedAt = DateTime.UtcNow;
                
                await _txRepo.AddAsync(new BottleTransaction
                {
                    BottleTypeId = bottleTypeId,
                    CustomerId = request.CustomerId,
                    TransactionType = BottleTransactionType.Issued,
                    Quantity = item.Quantity,
                    DepositAmount = item.BottleDepositAmount, // Using actual deposit provided
                    ReferenceType = "Sale",
                    ReferenceId = request.SaleId,
                    CreatedBy = request.User,
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            inventory.LastUpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
