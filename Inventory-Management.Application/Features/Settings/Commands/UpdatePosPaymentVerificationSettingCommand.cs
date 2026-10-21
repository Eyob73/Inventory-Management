using Inventory_Management.Application.DTOs.Settings;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Interfaces.Repositories;
using MediatR;

namespace Inventory_Management.Application.Features.Settings.Commands;

public record UpdatePosPaymentVerificationSettingCommand(Guid TenantId, bool Enabled) : IRequest<PosPaymentVerificationSettingDto>;

public class UpdatePosPaymentVerificationSettingCommandHandler : IRequestHandler<UpdatePosPaymentVerificationSettingCommand, PosPaymentVerificationSettingDto>
{
    private readonly IGenericRepository<Tenant> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePosPaymentVerificationSettingCommandHandler(
        IGenericRepository<Tenant> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PosPaymentVerificationSettingDto> Handle(UpdatePosPaymentVerificationSettingCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(request.TenantId);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant with ID {request.TenantId} not found.");

        tenant.PosPaymentVerificationEnabled = request.Enabled;

        await _repository.UpdateAsync(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PosPaymentVerificationSettingDto
        {
            Enabled = tenant.PosPaymentVerificationEnabled
        };
    }
}
