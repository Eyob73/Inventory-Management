using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IGenericRepository<Product> productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(command.Id)
            ?? throw new KeyNotFoundException($"Product with ID {command.Id} not found.");

        await _productRepository.SoftDeleteAsync(product.Id);
        await _unitOfWork.SaveChangesAsync();
    }
}
