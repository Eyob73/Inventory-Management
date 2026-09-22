using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.BottleTypes.Commands;

public class DeleteBottleTypeCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteBottleTypeCommandHandler : IRequestHandler<DeleteBottleTypeCommand, bool>
{
    private readonly IGenericRepository<BottleType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBottleTypeCommandHandler(IGenericRepository<BottleType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteBottleTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) return false;

        await _repository.DeleteAsync(entity.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}
