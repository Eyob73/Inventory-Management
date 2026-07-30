using Inventory_Management.Application.DTOs.Customer;

namespace Inventory_Management.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<CustomerDto> GetByIdAsync(Guid id);
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto> AddAsync(CreateCustomerDto dto);
    Task<CustomerDto> UpdateAsync(UpdateCustomerDto dto);
    Task DeleteAsync(Guid id);
}
