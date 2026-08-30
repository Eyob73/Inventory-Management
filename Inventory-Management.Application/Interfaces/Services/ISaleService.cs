using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Sale;

namespace Inventory_Management.Application.Interfaces.Services;

public interface ISaleService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, string? userId, string? cashierName);
    Task<PagedResponse<SaleDto>> GetPagedSalesAsync(SaleFilterDto filter, string? currentUserId, string? currentRole);
    Task<SaleDto> GetByIdAsync(Guid id);
    Task CancelSaleAsync(Guid id, string? currentUserId, string? currentRole);
}
