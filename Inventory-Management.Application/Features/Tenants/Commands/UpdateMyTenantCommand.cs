using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;

namespace Inventory_Management.Application.Features.Tenants.Commands;

public record UpdateMyTenantCommand(Guid TenantId, UpdateMyTenantDto Dto) : IRequest<TenantDto>;

public class UpdateMyTenantCommandHandler : IRequestHandler<UpdateMyTenantCommand, TenantDto>
{
    private readonly IGenericRepository<Tenant> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMyTenantCommandHandler(
        IGenericRepository<Tenant> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantDto> Handle(UpdateMyTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(request.TenantId);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant with ID {request.TenantId} not found.");

        tenant.Name = request.Dto.Name;
        tenant.Address = request.Dto.Address;
        tenant.Phone = request.Dto.Phone;
        tenant.Email = request.Dto.Email;
        tenant.Website = request.Dto.Website;
        tenant.TaxId = request.Dto.TaxId;

        if (request.Dto.LowStockThreshold.HasValue)
        {
            tenant.LowStockThreshold = request.Dto.LowStockThreshold.Value;
        }

        if (request.Dto.EnableBottleManagement.HasValue)
        {
            tenant.EnableBottleManagement = request.Dto.EnableBottleManagement.Value;
        }

        await _repository.UpdateAsync(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            Address = tenant.Address,
            Phone = tenant.Phone,
            Email = tenant.Email,
            Website = tenant.Website,
            TaxId = tenant.TaxId,
            LowStockThreshold = tenant.LowStockThreshold,
            EnableBottleManagement = tenant.EnableBottleManagement,
            IsActive = tenant.IsActive,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt
        };
    }
}
