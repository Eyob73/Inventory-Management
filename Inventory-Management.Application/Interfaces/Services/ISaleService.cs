using Inventory_Management.Application.DTOs.Sale;

namespace Inventory_Management.Application.Interfaces.Services;

public interface ISaleService
{
    Task<SaleDto> GetByIdAsync(Guid id);
    Task<IEnumerable<SaleDto>> GetAllAsync();
    Task<SaleDto> AddAsync(CreateSaleDto dto);
    Task<SaleDto> UpdateAsync(UpdateSaleDto dto);
    Task DeleteAsync(Guid id);
}
