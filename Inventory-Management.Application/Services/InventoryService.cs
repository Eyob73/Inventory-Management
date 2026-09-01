using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Inventory;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<InventoryTransaction> _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IGenericRepository<Product> productRepository,
        IGenericRepository<InventoryTransaction> transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public Task IncreaseStockAsync(
        Guid productId,
        int quantity,
        InventoryTransactionType type,
        Guid? referenceId,
        string? referenceType,
        string? notes,
        string? createdBy)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to increase must be greater than zero.");

        return ApplyAsync(productId, quantity, type, referenceId, referenceType, notes, createdBy);
    }

    public Task DecreaseStockAsync(
        Guid productId,
        int quantity,
        InventoryTransactionType type,
        Guid? referenceId,
        string? referenceType,
        string? notes,
        string? createdBy)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to decrease must be greater than zero.");

        return ApplyAsync(productId, -quantity, type, referenceId, referenceType, notes, createdBy);
    }

    public async Task AdjustStockAsync(Guid productId, int delta, string? notes, string? createdBy)
    {
        if (delta == 0)
            throw new ArgumentException("Adjustment quantity cannot be zero.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await ApplyAsync(productId, delta, InventoryTransactionType.Adjustment, null, "Adjustment", notes, createdBy);
            await _unitOfWork.SaveChangesAsync();
        });
    }

    public async Task<PagedResponse<InventoryTransactionDto>> GetTransactionsAsync(InventoryTransactionFilterDto filter)
    {
        var page = Math.Max(1, filter.PageIndex);
        var pageSize = Math.Clamp(filter.PageSize <= 0 ? 20 : filter.PageSize, 1, 100);

        var query = _transactionRepository.Query()
            .AsNoTracking()
            .Include(t => t.Product)
            .AsQueryable();

        if (filter.ProductId.HasValue && filter.ProductId != Guid.Empty)
            query = query.Where(t => t.ProductId == filter.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Type)
            && Enum.TryParse<InventoryTransactionType>(filter.Type, true, out var type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (filter.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt <= end);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(t =>
                (t.Product != null && (t.Product.Name.ToLower().Contains(term) || t.Product.SKU.ToLower().Contains(term)))
                || (t.Notes != null && t.Notes.ToLower().Contains(term))
                || (t.ReferenceType != null && t.ReferenceType.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<InventoryTransactionDto>
        {
            Items = items.Select(MapTransaction).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductStockDto> GetProductStockAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product with ID {productId} was not found.");

        var recent = await _transactionRepository.Query()
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        return new ProductStockDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            QuantityInStock = product.QuantityInStock,
            MinimumStock = product.MinimumStock,
            RecentTransactions = recent.Select(MapTransaction).ToList()
        };
    }

    private async Task ApplyAsync(
        Guid productId,
        int delta,
        InventoryTransactionType type,
        Guid? referenceId,
        string? referenceType,
        string? notes,
        string? createdBy)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product with ID {productId} was not found.");

        var previous = product.QuantityInStock;
        var next = previous + delta;

        if (next < 0)
        {
            throw new InvalidOperationException(
                $"Insufficient stock for product '{product.Name}'. Available: {previous}, requested change: {delta}.");
        }

        product.QuantityInStock = next;
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product);

        await _transactionRepository.AddAsync(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Type = type,
            Quantity = delta,
            PreviousQuantity = previous,
            NewQuantity = next,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        });
    }

    private static InventoryTransactionDto MapTransaction(InventoryTransaction t) => new()
    {
        Id = t.Id,
        ProductId = t.ProductId,
        ProductName = t.Product?.Name ?? string.Empty,
        SKU = t.Product?.SKU ?? string.Empty,
        Type = t.Type.ToString(),
        Quantity = t.Quantity,
        PreviousQuantity = t.PreviousQuantity,
        NewQuantity = t.NewQuantity,
        ReferenceId = t.ReferenceId,
        ReferenceType = t.ReferenceType,
        Notes = t.Notes,
        CreatedAt = t.CreatedAt,
        CreatedBy = t.CreatedBy
    };
}
