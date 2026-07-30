using Inventory_Management.Application.DTOs.Supplier;

namespace Inventory_Management.Application.Interfaces.Services;

public interface ISupplierService
{
    Task<SupplierDto> GetByIdAsync(Guid id);
    Task<IEnumerable<SupplierDto>> GetAllAsync();
    Task<SupplierDto> AddAsync(CreateSupplierDto dto);
    Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto);
    Task DeleteAsync(Guid id);
}
