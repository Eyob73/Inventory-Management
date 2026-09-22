using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using MediatR;

namespace Inventory_Management.Application.Features.SystemAdmin.Commands;

public record UpdateTenantStatusCommand(Guid Id, TenantStatus Status) : IRequest<bool>;

public class UpdateTenantStatusCommandHandler : IRequestHandler<UpdateTenantStatusCommand, bool>
{
    private readonly IGenericRepository<Tenant> _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantStatusCommandHandler(IGenericRepository<Tenant> tenantRepository, IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateTenantStatusCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id);
        if (tenant == null) return false;

        tenant.Status = request.Status;
        tenant.IsActive = request.Status == TenantStatus.Active;
        await _tenantRepository.UpdateAsync(tenant);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
