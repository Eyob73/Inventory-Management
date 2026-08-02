using Inventory_Management.Application.DTOs.Supplier;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IGenericRepository<Supplier> _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IGenericRepository<Supplier> supplierRepository, IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SupplierDto> GetByIdAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Supplier with ID {id} not found.");
        return MapToDto(supplier);
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierDto> AddAsync(CreateSupplierDto dto)
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ContactName = dto.ContactName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            CreatedAt = DateTime.UtcNow
        };
        await _supplierRepository.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto)
    {
        var supplier = await _supplierRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Supplier with ID {dto.Id} not found.");

        supplier.Name = dto.Name;
        supplier.ContactName = dto.ContactName;
        supplier.Email = dto.Email;
        supplier.PhoneNumber = dto.PhoneNumber;
        supplier.Address = dto.Address;

        await _supplierRepository.UpdateAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(supplier);
    }

    public async Task DeleteAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Supplier with ID {id} not found.");

        await _supplierRepository.DeleteAsync(supplier.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static SupplierDto MapToDto(Supplier s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        ContactName = s.ContactName,
        Email = s.Email,
        PhoneNumber = s.PhoneNumber,
        Address = s.Address,
        CreatedAt = s.CreatedAt
    };
}
