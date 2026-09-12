using Inventory_Management.Application.DTOs.Auth;
using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using MediatR;

namespace Inventory_Management.Application.Features.SystemAdmin.Commands;

public record CreateTenantWithAdminCommand(CreateTenantWithAdminDto Dto) : IRequest<TenantDto>;

public class CreateTenantWithAdminCommandHandler : IRequestHandler<CreateTenantWithAdminCommand, TenantDto>
{
    private readonly IGenericRepository<Tenant> _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;

    public CreateTenantWithAdminCommandHandler(
        IGenericRepository<Tenant> tenantRepository,
        IUnitOfWork unitOfWork,
        IAuthService authService)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _authService = authService;
    }

    public async Task<TenantDto> Handle(CreateTenantWithAdminCommand command, CancellationToken cancellationToken)
    {
        var dto = command.Dto;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code.ToLowerInvariant().Trim(),
            IsActive = true,
            Status = Domain.Enums.TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _tenantRepository.AddAsync(tenant);
        await _unitOfWork.SaveChangesAsync();

        var registerReq = new RegisterRequestDto(
            dto.AdminEmail,
            dto.AdminPassword,
            dto.AdminFirstName,
            dto.AdminLastName,
            "Admin",
            tenant.Id
        );

        var (success, errors, alreadyExists) = await _authService.RegisterAsync(registerReq);

        if (!success || alreadyExists)
        {
            await _tenantRepository.DeleteAsync(tenant.Id);
            await _unitOfWork.SaveChangesAsync();
            throw new Exception("Failed to create admin user: " + string.Join(", ", errors ?? Array.Empty<string>()));
        }

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            IsActive = tenant.IsActive,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt
        };
    }
}
