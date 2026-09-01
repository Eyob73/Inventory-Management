using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Interfaces.Repositories;

public interface IPurchaseRepository : IGenericRepository<Purchase>
{
    Task<Purchase?> GetWithItemsByIdAsync(Guid id);
    Task<string> GenerateUniquePurchaseNumberAsync();
}
