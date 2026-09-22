using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Features.SystemAdmin.Queries;

public record GetTenantUsersQuery(Guid TenantId) : IRequest<IEnumerable<TenantUserDto>>;

public class GetTenantUsersQueryHandler : IRequestHandler<GetTenantUsersQuery, IEnumerable<TenantUserDto>>
{
    private readonly UserManager<AppUser> _userManager;

    public GetTenantUsersQueryHandler(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IEnumerable<TenantUserDto>> Handle(GetTenantUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .Where(u => u.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        var dtos = new List<TenantUserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new TenantUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty
            });
        }
        return dtos;
    }
}
