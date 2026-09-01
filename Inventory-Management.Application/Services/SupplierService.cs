using System.Net.Mail;
using Inventory_Management.Application.DTOs.Supplier;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IGenericRepository<Supplier> _supplierRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(
        IGenericRepository<Supplier> supplierRepository,
        IPurchaseRepository purchaseRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _purchaseRepository = purchaseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SupplierDetailDto> GetByIdAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Supplier with ID {id} not found.");

        var purchases = await _purchaseRepository.Query()
            .AsNoTracking()
            .Include(p => p.PurchaseItems)
                .ThenInclude(i => i.Product)
            .Where(p => p.SupplierId == id)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();

        var completed = purchases.Where(p => p.Status == PurchaseStatus.Completed).ToList();

        var products = completed
            .SelectMany(p => p.PurchaseItems)
            .GroupBy(i => i.ProductId)
            .Select(g => new SupplierPurchasedProductDto
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? string.Empty,
                SKU = g.First().Product?.SKU ?? string.Empty,
                TotalQuantity = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(p => p.TotalQuantity)
            .ToList();

        return new SupplierDetailDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Email = supplier.Email,
            PhoneNumber = supplier.PhoneNumber,
            Address = supplier.Address,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt,
            TotalPurchasesCount = completed.Count,
            TotalPurchased = completed.Sum(p => p.TotalAmount),
            PurchasedProducts = products,
            PurchaseHistory = purchases.Select(p => new SupplierPurchaseHistoryDto
            {
                Id = p.Id,
                PurchaseNumber = p.PurchaseNumber,
                PurchaseDate = p.PurchaseDate,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                ItemsCount = p.PurchaseItems.Count
            }).ToList()
        };
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        var purchases = await _purchaseRepository.Query()
            .AsNoTracking()
            .Where(p => p.Status == PurchaseStatus.Completed)
            .Select(p => new { p.SupplierId, p.TotalAmount })
            .ToListAsync();

        return suppliers.Select(s =>
        {
            var sPurchases = purchases.Where(p => p.SupplierId == s.Id).ToList();
            return MapToDto(s, sPurchases.Count, sPurchases.Sum(p => p.TotalAmount));
        });
    }

    public async Task<SupplierDto> AddAsync(CreateSupplierDto dto)
    {
        Validate(dto.Name, dto.PhoneNumber, dto.Email);

        var existing = await _supplierRepository.GetAllAsync();
        EnsureUnique(existing, dto.PhoneNumber, dto.Email, null);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            ContactName = dto.ContactName?.Trim() ?? string.Empty,
            Email = dto.Email?.Trim() ?? string.Empty,
            PhoneNumber = dto.PhoneNumber.Trim(),
            Address = dto.Address?.Trim() ?? string.Empty,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _supplierRepository.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(supplier, 0, 0);
    }

    public async Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto)
    {
        Validate(dto.Name, dto.PhoneNumber, dto.Email);

        var supplier = await _supplierRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Supplier with ID {dto.Id} not found.");

        var existing = await _supplierRepository.GetAllAsync();
        EnsureUnique(existing, dto.PhoneNumber, dto.Email, dto.Id);

        supplier.Name = dto.Name.Trim();
        supplier.ContactName = dto.ContactName?.Trim() ?? string.Empty;
        supplier.Email = dto.Email?.Trim() ?? string.Empty;
        supplier.PhoneNumber = dto.PhoneNumber.Trim();
        supplier.Address = dto.Address?.Trim() ?? string.Empty;
        supplier.IsActive = dto.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(supplier, 0, 0);
    }

    public async Task DeleteAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Supplier with ID {id} not found.");

        var hasPurchases = await _purchaseRepository.Query().AnyAsync(p => p.SupplierId == id);
        if (hasPurchases)
        {
            throw new InvalidOperationException(
                $"Cannot delete supplier '{supplier.Name}' because they have associated purchase records. Mark the supplier as inactive instead.");
        }

        await _supplierRepository.SoftDeleteAsync(supplier.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static void Validate(string name, string phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Supplier name is required.");

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Supplier phone number is required.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(), @"^[+]?[(]?[0-9]{1,4}[)]?[-\s./0-9]*$"))
            throw new ArgumentException("Enter a valid phone number.");

        if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            throw new ArgumentException("Enter a valid email address.");
    }

    private static void EnsureUnique(IEnumerable<Supplier> existing, string phone, string? email, Guid? excludeId)
    {
        var trimmedPhone = phone.Trim();
        if (existing.Any(s => s.Id != excludeId && s.PhoneNumber.Trim().Equals(trimmedPhone, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"A supplier with phone number '{trimmedPhone}' already exists.");

        if (string.IsNullOrWhiteSpace(email))
            return;

        var trimmedEmail = email.Trim();
        if (existing.Any(s =>
                s.Id != excludeId
                && !string.IsNullOrEmpty(s.Email)
                && s.Email.Trim().Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"A supplier with email address '{trimmedEmail}' already exists.");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static SupplierDto MapToDto(Supplier s, int count, decimal total) => new()
    {
        Id = s.Id,
        Name = s.Name,
        ContactName = s.ContactName,
        Email = s.Email,
        PhoneNumber = s.PhoneNumber,
        Address = s.Address,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        TotalPurchasesCount = count,
        TotalPurchased = total
    };
}
