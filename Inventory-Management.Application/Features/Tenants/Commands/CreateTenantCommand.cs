using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.Tenants.Commands;

public record CreateTenantCommand(CreateTenantDto Dto) : IRequest<TenantDto>;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly IGenericRepository<Tenant> _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantCommandHandler(IGenericRepository<Tenant> tenantRepository, IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var dto = command.Dto;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code.ToLowerInvariant().Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _tenantRepository.AddAsync(tenant);
        await _unitOfWork.SaveChangesAsync();

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt
        };
    }
}
