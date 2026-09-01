using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Persistence.Repositories;

public class PurchaseRepository : GenericRepository<Purchase>, IPurchaseRepository
{
    public PurchaseRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Purchase?> GetWithItemsByIdAsync(Guid id)
    {
        return await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<string> GenerateUniquePurchaseNumberAsync()
    {
        string prefix = $"PO-{DateTime.UtcNow:yyyyMMdd}-";
        int randomSuffix = Random.Shared.Next(1000, 9999);
        string candidate = $"{prefix}{randomSuffix}";

        int retries = 0;
        while (await _context.Purchases.AnyAsync(p => p.PurchaseNumber == candidate) && retries < 10)
        {
            randomSuffix = Random.Shared.Next(1000, 9999);
            candidate = $"{prefix}{randomSuffix}";
            retries++;
        }

        return candidate;
    }
}
