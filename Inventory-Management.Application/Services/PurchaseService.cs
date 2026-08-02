using Inventory_Management.Application.DTOs.Purchase;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IGenericRepository<Purchase> _purchaseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseService(IGenericRepository<Purchase> purchaseRepository, IUnitOfWork unitOfWork)
    {
        _purchaseRepository = purchaseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseDto> GetByIdAsync(Guid id)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase with ID {id} not found.");
        return MapToDto(purchase);
    }

    public async Task<IEnumerable<PurchaseDto>> GetAllAsync()
    {
        var purchases = await _purchaseRepository.GetAllAsync();
        return purchases.Select(MapToDto);
    }

    public async Task<PurchaseDto> AddAsync(CreatePurchaseDto dto)
    {
        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            TotalAmount = dto.TotalAmount,
            SupplierId = dto.SupplierId,
            PurchaseDate = DateTime.UtcNow
        };
        await _purchaseRepository.AddAsync(purchase);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(purchase);
    }

    public async Task<PurchaseDto> UpdateAsync(UpdatePurchaseDto dto)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Purchase with ID {dto.Id} not found.");

        purchase.TotalAmount = dto.TotalAmount;
        purchase.SupplierId = dto.SupplierId;

        await _purchaseRepository.UpdateAsync(purchase);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(purchase);
    }

    public async Task DeleteAsync(Guid id)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase with ID {id} not found.");

        await _purchaseRepository.DeleteAsync(purchase.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static PurchaseDto MapToDto(Purchase p) => new()
    {
        Id = p.Id,
        PurchaseDate = p.PurchaseDate,
        TotalAmount = p.TotalAmount,
        SupplierId = p.SupplierId
    };
}
