using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Infrastructure.Persistence.Data;

namespace Inventory_Management.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
}
