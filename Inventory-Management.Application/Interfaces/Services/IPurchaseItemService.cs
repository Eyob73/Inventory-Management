using Inventory_Management.Application.DTOs.PurchaseItem;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IPurchaseItemService
{
    Task<PurchaseItemDto> GetByIdAsync(Guid id);
    Task<IEnumerable<PurchaseItemDto>> GetAllAsync();
    Task<PurchaseItemDto> AddAsync(CreatePurchaseItemDto dto);
    Task<PurchaseItemDto> UpdateAsync(UpdatePurchaseItemDto dto);
    Task DeleteAsync(Guid id);
}
