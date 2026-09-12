using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.SystemAdmin.Queries;

public record GetSystemDashboardQuery : IRequest<SystemDashboardDto>;

public class GetSystemDashboardQueryHandler : IRequestHandler<GetSystemDashboardQuery, SystemDashboardDto>
{
    private readonly IGenericRepository<Tenant> _tenantRepository;
    private readonly UserManager<AppUser> _userManager;

    public GetSystemDashboardQueryHandler(
        IGenericRepository<Tenant> tenantRepository,
        UserManager<AppUser> userManager)
    {
        _tenantRepository = tenantRepository;
        _userManager = userManager;
    }

    public async Task<SystemDashboardDto> Handle(GetSystemDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.GetAllAsync();
        var users = await _userManager.Users.ToListAsync(cancellationToken);

        return new SystemDashboardDto
        {
            TotalCompanies = tenants.Count(),
            ActiveCompanies = tenants.Count(t => t.Status == TenantStatus.Active),
            SuspendedCompanies = tenants.Count(t => t.Status == TenantStatus.Suspended),
            TotalUsers = users.Count,
            ActiveUsers = users.Count // We could add IsActive to AppUser if we wanted
        };
    }
}
