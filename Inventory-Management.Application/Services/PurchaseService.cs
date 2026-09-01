using Inventory_Management.Application.DTOs.Purchase;
using Inventory_Management.Application.DTOs.PurchaseItem;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IGenericRepository<PurchaseItem> _purchaseItemRepository;
    private readonly IGenericRepository<Supplier> _supplierRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        IGenericRepository<PurchaseItem> purchaseItemRepository,
        IGenericRepository<Supplier> supplierRepository,
        IGenericRepository<Product> productRepository,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork)
    {
        _purchaseRepository = purchaseRepository;
        _purchaseItemRepository = purchaseItemRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseDto> GetByIdAsync(Guid id)
    {
        var purchase = await _purchaseRepository.GetWithItemsByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase with ID {id} not found.");
        return MapToDto(purchase);
    }

    public async Task<IEnumerable<PurchaseDto>> GetAllAsync()
    {
        var purchases = await _purchaseRepository.Query()
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems)
                .ThenInclude(i => i.Product)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();

        return purchases.Select(MapToDto);
    }

    public async Task<PurchaseDto> AddAsync(CreatePurchaseDto dto, string? createdBy)
    {
        var lines = await ValidateLines(dto.Items);
        var supplier = await RequireActiveSupplierAsync(dto.SupplierId);

        Purchase? created = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var purchase = new Purchase
            {
                Id = Guid.NewGuid(),
                PurchaseNumber = await _purchaseRepository.GenerateUniquePurchaseNumberAsync(),
                SupplierId = supplier?.Id,
                PurchaseDate = NormalizeDate(dto.PurchaseDate),
                Status = PurchaseStatus.Draft,
                Notes = dto.Notes?.Trim(),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            ApplyLines(purchase, lines);
            await _purchaseRepository.AddAsync(purchase);
            await _unitOfWork.SaveChangesAsync();
            created = purchase;
        });

        return await GetByIdAsync(created!.Id);
    }

    public async Task<PurchaseDto> UpdateAsync(UpdatePurchaseDto dto)
    {
        var lines = await ValidateLines(dto.Items);
        var supplier = await RequireActiveSupplierAsync(dto.SupplierId);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var purchase = await _purchaseRepository.GetWithItemsByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException($"Purchase with ID {dto.Id} not found.");

            EnsureDraft(purchase, "Only draft purchases can be edited.");

            purchase.SupplierId = supplier?.Id;
            purchase.PurchaseDate = NormalizeDate(dto.PurchaseDate);
            purchase.Notes = dto.Notes?.Trim();
            purchase.UpdatedAt = DateTime.UtcNow;

            foreach (var existing in purchase.PurchaseItems.ToList())
                await _purchaseItemRepository.DeleteAsync(existing.Id);

            purchase.PurchaseItems.Clear();
            ApplyLines(purchase, lines);

            await _purchaseRepository.UpdateAsync(purchase);
            await _unitOfWork.SaveChangesAsync();
        });

        return await GetByIdAsync(dto.Id);
    }

    public async Task<PurchaseDto> CompleteAsync(Guid id, string? createdBy)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var purchase = await _purchaseRepository.GetWithItemsByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase with ID {id} not found.");

            EnsureDraft(purchase, "Only draft purchases can be completed.");

            if (purchase.PurchaseItems.Count == 0)
                throw new InvalidOperationException("A purchase must contain at least one item before it can be completed.");

            if (purchase.SupplierId.HasValue)
                await RequireActiveSupplierAsync(purchase.SupplierId);

            foreach (var item in purchase.PurchaseItems)
            {
                item.TotalCost = item.Quantity * item.UnitCost;
                await _inventoryService.IncreaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    InventoryTransactionType.Purchase,
                    purchase.Id,
                    "Purchase",
                    $"Purchase {purchase.PurchaseNumber}",
                    createdBy);
            }

            purchase.TotalAmount = purchase.PurchaseItems.Sum(i => i.TotalCost);
            purchase.Status = PurchaseStatus.Completed;
            purchase.UpdatedAt = DateTime.UtcNow;

            await _purchaseRepository.UpdateAsync(purchase);
            await _unitOfWork.SaveChangesAsync();
        });

        return await GetByIdAsync(id);
    }

    public async Task CancelAsync(Guid id, string? createdBy)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var purchase = await _purchaseRepository.GetWithItemsByIdAsync(id)
                ?? throw new KeyNotFoundException($"Purchase with ID {id} not found.");

            if (purchase.Status == PurchaseStatus.Cancelled)
                throw new InvalidOperationException($"Purchase '{purchase.PurchaseNumber}' is already cancelled.");

            if (purchase.Status == PurchaseStatus.Completed)
            {
                foreach (var item in purchase.PurchaseItems)
                {
                    await _inventoryService.DecreaseStockAsync(
                        item.ProductId,
                        item.Quantity,
                        InventoryTransactionType.PurchaseReversal,
                        purchase.Id,
                        "PurchaseReversal",
                        $"Reversal of purchase {purchase.PurchaseNumber}",
                        createdBy);
                }
            }

            purchase.Status = PurchaseStatus.Cancelled;
            purchase.UpdatedAt = DateTime.UtcNow;
            await _purchaseRepository.UpdateAsync(purchase);
            await _unitOfWork.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(Guid id)
    {
        var purchase = await _purchaseRepository.GetWithItemsByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase with ID {id} not found.");

        EnsureDraft(purchase, "Only draft purchases can be deleted. Completed purchases are historical records.");

        await _purchaseRepository.DeleteAsync(purchase.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<Supplier?> RequireActiveSupplierAsync(Guid? supplierId)
    {
        if (!supplierId.HasValue || supplierId.Value == Guid.Empty)
            return null;

        var supplier = await _supplierRepository.GetByIdAsync(supplierId.Value)
            ?? throw new KeyNotFoundException($"Supplier with ID {supplierId} was not found.");

        if (!supplier.IsActive)
            throw new InvalidOperationException($"Supplier '{supplier.Name}' is inactive and cannot be used for purchases.");

        return supplier;
    }

    private async Task<List<(Product Product, PurchaseLineInputDto Line)>> ValidateLines(List<PurchaseLineInputDto> items)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("A purchase must contain at least one item.");

        var productIds = items.Select(i => i.ProductId).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
            throw new ArgumentException("Duplicate products are not allowed on a purchase.");

        var validated = new List<(Product, PurchaseLineInputDto)>();
        foreach (var line in items)
        {
            if (line.Quantity <= 0)
                throw new ArgumentException("Item quantity must be greater than zero.");

            var product = await _productRepository.GetByIdAsync(line.ProductId)
                ?? throw new KeyNotFoundException($"Product with ID {line.ProductId} was not found.");

            if (product.Cost < 0)
                throw new InvalidOperationException($"Product '{product.Name}' has an invalid cost.");

            validated.Add((product, line));
        }

        return validated;
    }

    private static void ApplyLines(Purchase purchase, List<(Product Product, PurchaseLineInputDto Line)> lines)
    {
        decimal total = 0m;
        foreach (var (product, line) in lines)
        {
            var unitCost = product.Cost;
            var totalCost = line.Quantity * unitCost;
            total += totalCost;
            purchase.PurchaseItems.Add(new PurchaseItem
            {
                Id = Guid.NewGuid(),
                PurchaseId = purchase.Id,
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitCost = unitCost,
                TotalCost = totalCost
            });
        }

        purchase.TotalAmount = total;
    }

    private static void EnsureDraft(Purchase purchase, string message)
    {
        if (!string.Equals(purchase.Status, PurchaseStatus.Draft, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(message);
    }

    private static DateTime NormalizeDate(DateTime? date)
    {
        var value = date ?? DateTime.UtcNow;
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    private static PurchaseDto MapToDto(Purchase p) => new()
    {
        Id = p.Id,
        PurchaseNumber = p.PurchaseNumber,
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name,
        PurchaseDate = p.PurchaseDate,
        TotalAmount = p.TotalAmount,
        Status = p.Status,
        Notes = p.Notes,
        CreatedBy = p.CreatedBy,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Items = p.PurchaseItems.Select(i => new PurchaseItemDto
        {
            Id = i.Id,
            PurchaseId = i.PurchaseId,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            SKU = i.Product?.SKU ?? string.Empty,
            Quantity = i.Quantity,
            UnitCost = i.UnitCost,
            TotalCost = i.TotalCost
        }).ToList()
    };
}
