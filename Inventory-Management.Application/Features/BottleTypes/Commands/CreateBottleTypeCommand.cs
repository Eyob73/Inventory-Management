using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;

namespace Inventory_Management.Application.Features.BottleTypes.Commands;

public class CreateBottleTypeCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public string Capacity { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class CreateBottleTypeCommandHandler : IRequestHandler<CreateBottleTypeCommand, Guid>
{
    private readonly IGenericRepository<BottleType> _repository;
    private readonly IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> _inventoryRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBottleTypeCommandHandler(IGenericRepository<BottleType> repository, IGenericRepository<Inventory_Management.Domain.Entities.BottleInventory> inventoryRepo, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _inventoryRepo = inventoryRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateBottleTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new BottleType
        {
            Name = request.Name,
            Description = request.Description,
            DepositAmount = request.DepositAmount,
            Capacity = request.Capacity,
            Material = request.Material,
            IsActive = true
        };

                await _repository.AddAsync(entity);
        
        var inventory = new Inventory_Management.Domain.Entities.BottleInventory
        {
            BottleTypeId = entity.Id,
            EmptyBottles = request.Quantity,
            FullBottles = 0,
            DamagedBottles = 0,
            LostBottles = 0,
            WithCustomers = 0
        };
        await _inventoryRepo.AddAsync(inventory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}


