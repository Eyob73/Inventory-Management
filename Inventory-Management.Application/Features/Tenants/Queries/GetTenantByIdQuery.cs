using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.Tenants.Queries;

public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto>;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly IGenericRepository<Tenant> _tenantRepository;

    public GetTenantByIdQueryHandler(IGenericRepository<Tenant> tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var t = await _tenantRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Tenant with ID {request.Id} not found.");

        return new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt
        };
    }
}
