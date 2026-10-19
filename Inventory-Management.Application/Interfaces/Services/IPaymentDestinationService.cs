using Inventory_Management.Application.DTOs.Payment;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IPaymentDestinationService
{
    Task<List<PaymentDestinationDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<PaymentDestinationDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentDestinationDto> CreateAsync(CreatePaymentDestinationDto dto, string? userId, CancellationToken cancellationToken = default);
    Task<PaymentDestinationDto> UpdateAsync(Guid id, UpdatePaymentDestinationDto dto, CancellationToken cancellationToken = default);
    Task SetDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ChekiBankDto>> GetSupportedBanksAsync(CancellationToken cancellationToken = default);
}
