using Inventory_Management.Application.DTOs.User;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto> AddAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(UpdateUserDto dto);
    Task DeleteAsync(Guid id);
}
