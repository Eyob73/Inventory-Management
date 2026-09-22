using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.Tenants.Queries;

public record GetPagedTenantsQuery(PagedRequest Request) : IRequest<PagedResponse<TenantDto>>;

public class GetPagedTenantsQueryHandler : IRequestHandler<GetPagedTenantsQuery, PagedResponse<TenantDto>>
{
    private readonly IGenericRepository<Tenant> _tenantRepository;

    public GetPagedTenantsQueryHandler(IGenericRepository<Tenant> tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<PagedResponse<TenantDto>> Handle(GetPagedTenantsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Request.Page;
        var pageSize = request.Request.PageSize;
        var search = request.Request.Search?.ToLower();

        Func<IQueryable<Tenant>, IOrderedQueryable<Tenant>> orderBy = q =>
        {
            var sort = request.Request.OrderBy?.ToLower() ?? "createdat";
            var desc = request.Request.Descending;
            
            return sort switch
            {
                "name" => desc ? q.OrderByDescending(t => t.Name) : q.OrderBy(t => t.Name),
                "code" => desc ? q.OrderByDescending(t => t.Code) : q.OrderBy(t => t.Code),
                "status" => desc ? q.OrderByDescending(t => t.Status) : q.OrderBy(t => t.Status),
                _ => desc ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt)
            };
        };

        var statusFilter = request.Request.Status;
        
        var (items, totalCount) = await _tenantRepository.GetPagedAsync(
            page,
            pageSize,
            predicate: t => 
                (string.IsNullOrWhiteSpace(search) || t.Name.ToLower().Contains(search) || t.Code.ToLower().Contains(search)) &&
                (!statusFilter.HasValue || (int)t.Status == statusFilter.Value),
            orderBy: orderBy
        );

        var dtos = items.Select(t => new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            IsActive = t.IsActive,
            Status = t.Status,
            CreatedAt = t.CreatedAt
        }).ToList();

        return new PagedResponse<TenantDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
