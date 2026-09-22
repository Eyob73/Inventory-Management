using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Infrastructure.Persistence.Repositories;

public class SaleRepository : GenericRepository<Sale>, ISaleRepository
{
    public SaleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Sale?> GetWithItemsByIdAsync(Guid id)
    {
        return await _context.Sales
            .Include(s => s.SaleItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<PagedResponse<Sale>> GetPagedSalesAsync(SaleFilterDto filter, string? currentUserId, string? currentRole)
    {
        var query = _context.Sales
            .Include(s => s.SaleItems)
            .AsNoTracking()
            .AsQueryable();

        // Role-based filtering for Sales user
        if (!string.IsNullOrEmpty(currentRole) && currentRole.Equals("Sales", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(currentUserId))
            {
                query = query.Where(s => s.UserId == currentUserId);
            }
        }
        else if (!string.IsNullOrEmpty(filter.UserId))
        {
            query = query.Where(s => s.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.SaleNumber.ToLower().Contains(term) ||
                (s.CustomerName != null && s.CustomerName.ToLower().Contains(term)) ||
                (s.CashierName != null && s.CashierName.ToLower().Contains(term)) ||
                (s.Notes != null && s.Notes.ToLower().Contains(term)));
        }

        if (filter.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(s => s.SaleDate >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(s => s.SaleDate <= endUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.PaymentMethod))
        {
            query = query.Where(s => s.PaymentMethod == filter.PaymentMethod);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(s => s.Status == filter.Status);
        }

        int totalCount = await query.CountAsync();

        int pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
        int pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var items = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Sale>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<string> GenerateUniqueSaleNumberAsync()
    {
        string prefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
        int randomSuffix = Random.Shared.Next(1000, 9999);
        string candidate = $"{prefix}{randomSuffix}";

        int retries = 0;
        while (await _context.Sales.AnyAsync(s => s.SaleNumber == candidate) && retries < 10)
        {
            randomSuffix = Random.Shared.Next(1000, 9999);
            candidate = $"{prefix}{randomSuffix}";
            retries++;
        }

        return candidate;
    }
}
