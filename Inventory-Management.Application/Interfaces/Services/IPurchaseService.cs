using Inventory_Management.Application.DTOs.Purchase;
using Inventory_Management.Application.DTOs.PurchaseItem;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IPurchaseService
{
    Task<PurchaseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<PurchaseDto>> GetAllAsync();
    Task<PurchaseDto> AddAsync(CreatePurchaseDto dto, string? createdBy);
    Task<PurchaseDto> UpdateAsync(UpdatePurchaseDto dto);
    Task<PurchaseDto> CompleteAsync(Guid id, string? createdBy);
    Task CancelAsync(Guid id, string? createdBy);
    Task DeleteAsync(Guid id);
}
