using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Interfaces.Repositories;

public interface ISaleRepository : IGenericRepository<Sale>
{
    Task<Sale?> GetWithItemsByIdAsync(Guid id);
    Task<PagedResponse<Sale>> GetPagedSalesAsync(SaleFilterDto filter, string? currentUserId, string? currentRole);
    Task<string> GenerateUniqueSaleNumberAsync();
}
