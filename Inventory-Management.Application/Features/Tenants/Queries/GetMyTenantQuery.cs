using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;

namespace Inventory_Management.Application.Features.Tenants.Queries;

public record GetMyTenantQuery(Guid TenantId) : IRequest<TenantDto>;

public class GetMyTenantQueryHandler : IRequestHandler<GetMyTenantQuery, TenantDto>
{
    private readonly IGenericRepository<Tenant> _repository;

    public GetMyTenantQueryHandler(IGenericRepository<Tenant> repository)
    {
        _repository = repository;
    }

    public async Task<TenantDto> Handle(GetMyTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(request.TenantId);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant with ID {request.TenantId} not found.");

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
            IsActive = tenant.IsActive,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt
        };
    }
}
