using Inventory_Management.Application.DTOs.Role;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IRoleService
{
    Task<RoleDto> GetByIdAsync(Guid id);
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<RoleDto> AddAsync(CreateRoleDto dto);
    Task<RoleDto> UpdateAsync(UpdateRoleDto dto);
    Task DeleteAsync(Guid id);
}
