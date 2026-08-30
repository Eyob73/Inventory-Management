using Inventory_Management.Application.DTOs.SaleItem;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class SaleItemService : ISaleItemService
{
    private readonly IGenericRepository<SaleItem> _saleItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaleItemService(IGenericRepository<SaleItem> saleItemRepository, IUnitOfWork unitOfWork)
    {
        _saleItemRepository = saleItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaleItemDto> GetByIdAsync(Guid id)
    {
        var item = await _saleItemRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"SaleItem with ID {id} not found.");
        return MapToDto(item);
    }

    public async Task<IEnumerable<SaleItemDto>> GetAllAsync()
    {
        var items = await _saleItemRepository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<SaleItemDto> AddAsync(CreateSaleItemDto dto)
    {
        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            DiscountAmount = dto.DiscountAmount,
            Subtotal = dto.Quantity * dto.UnitPrice,
            TotalPrice = Math.Max(0m, (dto.Quantity * dto.UnitPrice) - dto.DiscountAmount)
        };
        await _saleItemRepository.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<SaleItemDto> UpdateAsync(UpdateSaleItemDto dto)
    {
        var item = await _saleItemRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"SaleItem with ID {dto.Id} not found.");

        item.ProductId = dto.ProductId;
        item.Quantity = dto.Quantity;
        item.UnitPrice = dto.UnitPrice;
        item.Subtotal = dto.Quantity * dto.UnitPrice;
        item.TotalPrice = Math.Max(0m, item.Subtotal - item.DiscountAmount);

        await _saleItemRepository.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _saleItemRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"SaleItem with ID {id} not found.");

        await _saleItemRepository.DeleteAsync(item.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static SaleItemDto MapToDto(SaleItem s) => new()
    {
        Id = s.Id,
        SaleId = s.SaleId,
        ProductId = s.ProductId,
        ProductName = s.ProductName,
        SKU = s.SKU,
        Quantity = s.Quantity,
        UnitPrice = s.UnitPrice,
        DiscountAmount = s.DiscountAmount,
        Subtotal = s.Subtotal,
        TotalPrice = s.TotalPrice
    };
}
