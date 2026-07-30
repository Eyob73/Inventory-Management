using Inventory_Management.Application.DTOs.Purchase;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IPurchaseService
{
    Task<PurchaseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<PurchaseDto>> GetAllAsync();
    Task<PurchaseDto> AddAsync(CreatePurchaseDto dto);
    Task<PurchaseDto> UpdateAsync(UpdatePurchaseDto dto);
    Task DeleteAsync(Guid id);
}
