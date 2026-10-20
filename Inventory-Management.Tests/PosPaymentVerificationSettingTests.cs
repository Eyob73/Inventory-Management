using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Application.DTOs.Settings;
using Inventory_Management.Application.DTOs.Tenant;
using Inventory_Management.Application.Features.Settings.Commands;
using Inventory_Management.Application.Features.Settings.Queries;
using Inventory_Management.Application.Features.Tenants.Commands;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Domain.Entities;
using Moq;
using Xunit;

namespace Inventory_Management.Tests;

public class PosPaymentVerificationSettingTests
{
    private readonly Mock<IGenericRepository<Tenant>> _tenantRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public PosPaymentVerificationSettingTests()
    {
        _tenantRepoMock = new Mock<IGenericRepository<Tenant>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task GetPosPaymentVerificationSetting_ReturnsPersistedValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            PosPaymentVerificationEnabled = true
        };

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        var handler = new GetPosPaymentVerificationSettingQueryHandler(_tenantRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetPosPaymentVerificationSettingQuery(tenantId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async Task GetPosPaymentVerificationSetting_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            PosPaymentVerificationEnabled = false
        };

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        var handler = new GetPosPaymentVerificationSettingQueryHandler(_tenantRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetPosPaymentVerificationSettingQuery(tenantId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Enabled);
    }

    [Fact]
    public async Task GetPosPaymentVerificationSetting_TenantNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync((Tenant?)null);

        var handler = new GetPosPaymentVerificationSettingQueryHandler(_tenantRepoMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetPosPaymentVerificationSettingQuery(tenantId), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePosPaymentVerificationSetting_PersistsNewState()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            PosPaymentVerificationEnabled = true
        };

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdatePosPaymentVerificationSettingCommandHandler(
            _tenantRepoMock.Object,
            _unitOfWorkMock.Object);

        // Act
        var result = await handler.Handle(new UpdatePosPaymentVerificationSettingCommand(tenantId, false), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Enabled);
        Assert.False(tenant.PosPaymentVerificationEnabled);
        _tenantRepoMock.Verify(r => r.UpdateAsync(tenant), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePosPaymentVerificationSetting_TenantNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync((Tenant?)null);

        var handler = new UpdatePosPaymentVerificationSettingCommandHandler(
            _tenantRepoMock.Object,
            _unitOfWorkMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new UpdatePosPaymentVerificationSettingCommand(tenantId, true), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMyTenant_WhenPosPaymentVerificationProvided_UpdatesAndReturns()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Old Tenant Name",
            PosPaymentVerificationEnabled = true
        };

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateMyTenantCommandHandler(
            _tenantRepoMock.Object,
            _unitOfWorkMock.Object);

        var dto = new UpdateMyTenantDto
        {
            Name = "New Tenant Name",
            PosPaymentVerificationEnabled = false
        };

        // Act
        var result = await handler.Handle(new UpdateMyTenantCommand(tenantId, dto), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.PosPaymentVerificationEnabled);
        Assert.False(tenant.PosPaymentVerificationEnabled);
        _tenantRepoMock.Verify(r => r.UpdateAsync(tenant), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
