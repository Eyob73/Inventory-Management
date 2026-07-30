using Inventory_Management.Application.DTOs.SaleItem;

namespace Inventory_Management.Application.Interfaces.Services;

public interface ISaleItemService
{
    Task<SaleItemDto> GetByIdAsync(Guid id);
    Task<IEnumerable<SaleItemDto>> GetAllAsync();
    Task<SaleItemDto> AddAsync(CreateSaleItemDto dto);
    Task<SaleItemDto> UpdateAsync(UpdateSaleItemDto dto);
    Task DeleteAsync(Guid id);
}
