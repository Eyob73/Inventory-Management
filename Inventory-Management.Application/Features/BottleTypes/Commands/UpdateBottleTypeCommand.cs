using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleTypes.Commands;

public class UpdateBottleTypeCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public string Capacity { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Quantity { get; set; }
}

public class UpdateBottleTypeCommandHandler : IRequestHandler<UpdateBottleTypeCommand, bool>
{
    private readonly IGenericRepository<BottleType> _repository;
    private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _inventoryRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBottleTypeCommandHandler(IGenericRepository<BottleType> repository, IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> inventoryRepo, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _inventoryRepo = inventoryRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateBottleTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) return false;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.DepositAmount = request.DepositAmount;
        entity.Capacity = request.Capacity;
        entity.Material = request.Material;
                entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        var inventory = await _inventoryRepo.Query().FirstOrDefaultAsync(x => x.BottleTypeId == request.Id, cancellationToken);
        if (inventory != null)
        {
            inventory.EmptyBottles = request.Quantity;
        }
        else
        {
            var newInventory = new Inventory_Management.Domain.Entities.BottleInventory
            {
                BottleTypeId = entity.Id,
                EmptyBottles = request.Quantity,
                FullBottles = 0,
                DamagedBottles = 0,
                LostBottles = 0,
                WithCustomers = 0
            };
            await _inventoryRepo.AddAsync(newInventory);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

