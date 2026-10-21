using Inventory_Management.Application.DTOs.Payment;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IChekiVerificationService
{
    Task<List<ChekiBankDto>> GetSupportedBanksAsync(CancellationToken cancellationToken = default);
    Task<ChekiVerifyResponse> VerifyPaymentAsync(ChekiVerifyRequest request, CancellationToken cancellationToken = default);
}
