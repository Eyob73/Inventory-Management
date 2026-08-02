using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class SaleService : ISaleService
{
    private readonly IGenericRepository<Sale> _saleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaleService(IGenericRepository<Sale> saleRepository, IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaleDto> GetByIdAsync(Guid id)
    {
        var sale = await _saleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Sale with ID {id} not found.");
        return MapToDto(sale);
    }

    public async Task<IEnumerable<SaleDto>> GetAllAsync()
    {
        var sales = await _saleRepository.GetAllAsync();
        return sales.Select(MapToDto);
    }

    public async Task<SaleDto> AddAsync(CreateSaleDto dto)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            TotalAmount = dto.TotalAmount,
            CustomerId = dto.CustomerId,
            SaleDate = DateTime.UtcNow
        };
        await _saleRepository.AddAsync(sale);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(sale);
    }

    public async Task<SaleDto> UpdateAsync(UpdateSaleDto dto)
    {
        var sale = await _saleRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Sale with ID {dto.Id} not found.");

        sale.TotalAmount = dto.TotalAmount;
        sale.CustomerId = dto.CustomerId;

        await _saleRepository.UpdateAsync(sale);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(sale);
    }

    public async Task DeleteAsync(Guid id)
    {
        var sale = await _saleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Sale with ID {id} not found.");

        await _saleRepository.DeleteAsync(sale.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static SaleDto MapToDto(Sale s) => new()
    {
        Id = s.Id,
        SaleDate = s.SaleDate,
        TotalAmount = s.TotalAmount,
        CustomerId = s.CustomerId
    };
}
