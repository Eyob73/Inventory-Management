using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.Tenants.Queries;

public record GetAllTenantsQuery : IRequest<IEnumerable<TenantDto>>;

public class GetAllTenantsQueryHandler : IRequestHandler<GetAllTenantsQuery, IEnumerable<TenantDto>>
{
    private readonly IGenericRepository<Tenant> _tenantRepository;

    public GetAllTenantsQueryHandler(IGenericRepository<Tenant> tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<IEnumerable<TenantDto>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.GetAllAsync();
        return tenants.Select(t => new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt
        });
    }
}
