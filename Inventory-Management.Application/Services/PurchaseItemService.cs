using Inventory_Management.Application.DTOs.PurchaseItem;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.Services;

public class PurchaseItemService : IPurchaseItemService
{
    private readonly IGenericRepository<PurchaseItem> _purchaseItemRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseItemService(
        IGenericRepository<PurchaseItem> purchaseItemRepository,
        IPurchaseRepository purchaseRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseItemRepository = purchaseItemRepository;
        _purchaseRepository = purchaseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseItemDto> GetByIdAsync(Guid id)
    {
        var item = await _purchaseItemRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"PurchaseItem with ID {id} not found.");
        return MapToDto(item);
    }

    public async Task<IEnumerable<PurchaseItemDto>> GetAllAsync()
    {
        var items = await _purchaseItemRepository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<PurchaseItemDto> AddAsync(CreatePurchaseItemDto dto)
    {
        await EnsureDraftPurchaseAsync(dto.PurchaseId);

        var item = new PurchaseItem
        {
            Id = Guid.NewGuid(),
            PurchaseId = dto.PurchaseId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            TotalCost = dto.Quantity * dto.UnitCost
        };
        await _purchaseItemRepository.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<PurchaseItemDto> UpdateAsync(UpdatePurchaseItemDto dto)
    {
        var item = await _purchaseItemRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"PurchaseItem with ID {dto.Id} not found.");

        await EnsureDraftPurchaseAsync(item.PurchaseId);

        item.PurchaseId = dto.PurchaseId;
        item.ProductId = dto.ProductId;
        item.Quantity = dto.Quantity;
        item.UnitCost = dto.UnitCost;
        item.TotalCost = dto.Quantity * dto.UnitCost;

        await _purchaseItemRepository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _purchaseItemRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"PurchaseItem with ID {id} not found.");

        await EnsureDraftPurchaseAsync(item.PurchaseId);
        await _purchaseItemRepository.DeleteAsync(item.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task EnsureDraftPurchaseAsync(Guid purchaseId)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId)
            ?? throw new KeyNotFoundException($"Purchase with ID {purchaseId} not found.");

        if (!string.Equals(purchase.Status, PurchaseStatus.Draft, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Purchase line items can only be changed on draft purchases.");
    }

    private static PurchaseItemDto MapToDto(PurchaseItem p) => new()
    {
        Id = p.Id,
        PurchaseId = p.PurchaseId,
        ProductId = p.ProductId,
        Quantity = p.Quantity,
        UnitCost = p.UnitCost,
        TotalCost = p.TotalCost
    };
}
