using Inventory_Management.Application.Common;
using Inventory_Management.Application.DTOs.Payment;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management.Application.Services;

public class PaymentDestinationService : IPaymentDestinationService
{
    private readonly IGenericRepository<PaymentDestination> _destinationRepository;
    private readonly IGenericRepository<PaymentVerification> _verificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChekiVerificationService _chekiService;

    public PaymentDestinationService(
        IGenericRepository<PaymentDestination> destinationRepository,
        IGenericRepository<PaymentVerification> verificationRepository,
        IUnitOfWork unitOfWork,
        IChekiVerificationService chekiService)
    {
        _destinationRepository = destinationRepository;
        _verificationRepository = verificationRepository;
        _unitOfWork = unitOfWork;
        _chekiService = chekiService;
    }

    public async Task<List<PaymentDestinationDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _destinationRepository.Query().AsNoTracking();

        if (activeOnly == true)
        {
            query = query.Where(d => d.IsActive);
        }

        var list = await query
            .OrderByDescending(d => d.IsDefault)
            .ThenByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<PaymentDestinationDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var destination = await _destinationRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment destination with ID '{id}' was not found.");

        return MapToDto(destination);
    }

    public async Task<PaymentDestinationDto> CreateAsync(CreatePaymentDestinationDto dto, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Destination name is required.", nameof(dto.Name));

        ValidateDestinationType(dto.Type, dto.BankCode, dto.AccountNumber, dto.PhoneNumber);

        string? normalizedPhone = null;
        if (dto.Type == PaymentDestinationType.Telebirr)
        {
            normalizedPhone = PaymentDestinationSecurity.NormalizePhoneNumber(dto.PhoneNumber);
        }

        if (dto.IsDefault)
        {
            await ClearOtherDefaultsAsync(null, cancellationToken);
        }

        var destination = new PaymentDestination
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Type = dto.Type,
            BankCode = dto.BankCode?.Trim().ToLowerInvariant(),
            BankName = dto.BankName?.Trim(),
            AccountNumber = dto.AccountNumber?.Trim(),
            PhoneNumber = normalizedPhone ?? dto.PhoneNumber?.Trim(),
            AccountHolderName = dto.AccountHolderName?.Trim(),
            IsDefault = dto.IsDefault,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _destinationRepository.AddAsync(destination);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(destination);
    }

    public async Task<PaymentDestinationDto> UpdateAsync(Guid id, UpdatePaymentDestinationDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var destination = await _destinationRepository.Query()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment destination with ID '{id}' was not found.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Destination name is required.", nameof(dto.Name));

        if (dto.IsDefault && !destination.IsDefault)
        {
            await ClearOtherDefaultsAsync(id, cancellationToken);
        }

        destination.Name = dto.Name.Trim();
        destination.BankCode = dto.BankCode?.Trim().ToLowerInvariant() ?? destination.BankCode;
        destination.BankName = dto.BankName?.Trim() ?? destination.BankName;
        destination.AccountHolderName = dto.AccountHolderName?.Trim();
        destination.IsDefault = dto.IsDefault;
        destination.IsActive = dto.IsActive;
        destination.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.AccountNumber) && !dto.AccountNumber.Contains('*'))
        {
            destination.AccountNumber = dto.AccountNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && !dto.PhoneNumber.Contains('*'))
        {
            destination.PhoneNumber = PaymentDestinationSecurity.NormalizePhoneNumber(dto.PhoneNumber);
        }

        if (!destination.IsActive)
        {
            destination.IsDefault = false;
        }

        await _destinationRepository.UpdateAsync(destination);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(destination);
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var destination = await _destinationRepository.Query()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment destination with ID '{id}' was not found.");

        if (!destination.IsActive)
            throw new InvalidOperationException("Cannot set an inactive payment destination as default.");

        await ClearOtherDefaultsAsync(id, cancellationToken);

        destination.IsDefault = true;
        destination.UpdatedAtUtc = DateTime.UtcNow;

        await _destinationRepository.UpdateAsync(destination);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var destination = await _destinationRepository.Query()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment destination with ID '{id}' was not found.");

        destination.IsActive = isActive;
        if (!isActive && destination.IsDefault)
        {
            destination.IsDefault = false;
        }
        destination.UpdatedAtUtc = DateTime.UtcNow;

        await _destinationRepository.UpdateAsync(destination);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var destination = await _destinationRepository.Query()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment destination with ID '{id}' was not found.");

        var isUsed = await _verificationRepository.Query()
            .AnyAsync(v => v.PaymentDestinationId == id, cancellationToken);

        if (isUsed)
        {
            destination.IsActive = false;
            destination.IsDefault = false;
            destination.IsDeleted = true;
            destination.DeletedAt = DateTime.UtcNow;
            destination.UpdatedAtUtc = DateTime.UtcNow;
            await _destinationRepository.UpdateAsync(destination);
        }
        else
        {
            await _destinationRepository.DeleteAsync(destination.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ChekiBankDto>> GetSupportedBanksAsync(CancellationToken cancellationToken = default)
    {
        return await _chekiService.GetSupportedBanksAsync(cancellationToken);
    }

    private async Task ClearOtherDefaultsAsync(Guid? excludeId, CancellationToken cancellationToken)
    {
        var defaults = await _destinationRepository.Query()
            .Where(d => d.IsDefault && (excludeId == null || d.Id != excludeId.Value))
            .ToListAsync(cancellationToken);

        foreach (var def in defaults)
        {
            def.IsDefault = false;
            def.UpdatedAtUtc = DateTime.UtcNow;
            await _destinationRepository.UpdateAsync(def);
        }
    }

    private static void ValidateDestinationType(PaymentDestinationType type, string? bankCode, string? accountNumber, string? phoneNumber)
    {
        if (type == PaymentDestinationType.BankAccount)
        {
            if (string.IsNullOrWhiteSpace(bankCode))
                throw new ArgumentException("Bank selection is required for a bank account destination.", nameof(bankCode));

            if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Trim().Length < 4)
                throw new ArgumentException("A valid account number is required for a bank account destination.", nameof(accountNumber));
        }
        else if (type == PaymentDestinationType.Telebirr)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Trim().Length < 9)
                throw new ArgumentException("A valid phone number is required for a Telebirr destination.", nameof(phoneNumber));
        }
    }

    private static PaymentDestinationDto MapToDto(PaymentDestination destination)
    {
        return new PaymentDestinationDto
        {
            Id = destination.Id,
            Name = destination.Name,
            Type = destination.Type,
            BankCode = destination.BankCode,
            BankName = destination.BankName,
            MaskedAccountNumber = PaymentDestinationSecurity.MaskAccountNumber(destination.AccountNumber),
            MaskedPhoneNumber = PaymentDestinationSecurity.MaskPhoneNumber(destination.PhoneNumber),
            AccountHolderName = destination.AccountHolderName,
            IsDefault = destination.IsDefault,
            IsActive = destination.IsActive,
            CreatedAtUtc = destination.CreatedAtUtc,
            UpdatedAtUtc = destination.UpdatedAtUtc
        };
    }
}
