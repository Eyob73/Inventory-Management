using Inventory_Management.Application.DTOs.Settings;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;

namespace Inventory_Management.Application.Features.Settings.Queries;

public record GetPosPaymentVerificationSettingQuery(Guid TenantId) : IRequest<PosPaymentVerificationSettingDto>;

public class GetPosPaymentVerificationSettingQueryHandler : IRequestHandler<GetPosPaymentVerificationSettingQuery, PosPaymentVerificationSettingDto>
{
    private readonly IGenericRepository<Tenant> _repository;

    public GetPosPaymentVerificationSettingQueryHandler(IGenericRepository<Tenant> repository)
    {
        _repository = repository;
    }

    public async Task<PosPaymentVerificationSettingDto> Handle(GetPosPaymentVerificationSettingQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(request.TenantId);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant with ID {request.TenantId} not found.");

        return new PosPaymentVerificationSettingDto
        {
            Enabled = tenant.PosPaymentVerificationEnabled
        };
    }
}
