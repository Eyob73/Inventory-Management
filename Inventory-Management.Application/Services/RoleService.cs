using Inventory_Management.Application.DTOs.Role;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class RoleService : IRoleService
{
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RoleService(IGenericRepository<Role> roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleDto> GetByIdAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Role with ID {id} not found.");
        return MapToDto(role);
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(MapToDto);
    }

    public async Task<RoleDto> AddAsync(CreateRoleDto dto)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description
        };
        await _roleRepository.AddAsync(role);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(role);
    }

    public async Task<RoleDto> UpdateAsync(UpdateRoleDto dto)
    {
        var role = await _roleRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Role with ID {dto.Id} not found.");

        role.Name = dto.Name;
        role.Description = dto.Description;

        await _roleRepository.UpdateAsync(role);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(role);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Role with ID {id} not found.");

        await _roleRepository.DeleteAsync(role.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static RoleDto MapToDto(Role r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description
    };
}
