using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Application.Common;
using Inventory_Management.Application.DTOs.Payment;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Application.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Moq;
using Xunit;

namespace Inventory_Management.Tests;

public class PaymentDestinationServiceTests
{
    private readonly Mock<IGenericRepository<PaymentDestination>> _destRepoMock;
    private readonly Mock<IGenericRepository<PaymentVerification>> _verifRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IChekiVerificationService> _chekiMock;
    private readonly PaymentDestinationService _service;

    public PaymentDestinationServiceTests()
    {
        _destRepoMock = new Mock<IGenericRepository<PaymentDestination>>();
        _verifRepoMock = new Mock<IGenericRepository<PaymentVerification>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _chekiMock = new Mock<IChekiVerificationService>();
        _destRepoMock.Setup(r => r.Query()).Returns(() => new List<PaymentDestination>().AsAsyncQueryable());

        _service = new PaymentDestinationService(
            _destRepoMock.Object,
            _verifRepoMock.Object,
            _unitOfWorkMock.Object,
            _chekiMock.Object);
    }

    [Fact]
    public async Task CreateAsync_BankAccount_ValidatesAndNormalizesDetails()
    {
        // Arrange
        var request = new CreatePaymentDestinationDto
        {
            Name = "CBE Main",
            Type = PaymentDestinationType.BankAccount,
            BankCode = "cbe",
            BankName = "Commercial Bank of Ethiopia",
            AccountNumber = " 1000123456789 ",
            IsDefault = true
        };

        PaymentDestination? savedEntity = null;
        _destRepoMock.Setup(r => r.AddAsync(It.IsAny<PaymentDestination>()))
            .Callback<PaymentDestination>(d => savedEntity = d)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateAsync(request, "test-user-1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(savedEntity);
        Assert.Equal("CBE Main", savedEntity.Name);
        Assert.Equal("1000123456789", savedEntity.AccountNumber);
        Assert.True(savedEntity.IsDefault);
        Assert.True(savedEntity.IsActive);
        Assert.Equal("cbe", savedEntity.BankCode);
        // Masked check
        Assert.Equal("1000****6789", result.MaskedAccountNumber);
    }

    [Fact]
    public async Task CreateAsync_Telebirr_NormalizesPhoneNumber()
    {
        // Arrange
        var request = new CreatePaymentDestinationDto
        {
            Name = "Cashier Telebirr",
            Type = PaymentDestinationType.Telebirr,
            PhoneNumber = "0911223344",
            IsDefault = false
        };

        PaymentDestination? savedEntity = null;
        _destRepoMock.Setup(r => r.AddAsync(It.IsAny<PaymentDestination>()))
            .Callback<PaymentDestination>(d => savedEntity = d)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateAsync(request, "test-user-1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(savedEntity);
        Assert.Equal("+251911223344", savedEntity.PhoneNumber);
        Assert.Equal("+2519****3344", result.MaskedPhoneNumber);
    }

    [Fact]
    public async Task CreateAsync_MissingBankAccountDetails_ThrowsArgumentException()
    {
        // Arrange: BankAccount with empty account number
        var request = new CreatePaymentDestinationDto
        {
            Name = "Incomplete Bank",
            Type = PaymentDestinationType.BankAccount,
            BankCode = "cbe",
            AccountNumber = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request, "test-user-1"));
    }

    [Fact]
    public async Task CreateAsync_MissingTelebirrPhone_ThrowsArgumentException()
    {
        // Arrange: Telebirr with empty phone
        var request = new CreatePaymentDestinationDto
        {
            Name = "Empty Telebirr",
            Type = PaymentDestinationType.Telebirr,
            PhoneNumber = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request, "test-user-1"));
    }

    [Fact]
    public void SecurityHelper_NormalizesEthiopianPhoneNumbers()
    {
        Assert.Equal("+251912345678", PaymentDestinationSecurity.NormalizePhoneNumber("0912345678"));
        Assert.Equal("+251712345678", PaymentDestinationSecurity.NormalizePhoneNumber("0712345678"));
        Assert.Equal("+251912345678", PaymentDestinationSecurity.NormalizePhoneNumber("+251912345678"));
        Assert.Equal("+251912345678", PaymentDestinationSecurity.NormalizePhoneNumber("251912345678"));
    }

    [Fact]
    public void SecurityHelper_MasksAccountNumbersProperly()
    {
        Assert.Equal("1000****6789", PaymentDestinationSecurity.MaskAccountNumber("1000123456789"));
        Assert.Equal("+2519****3344", PaymentDestinationSecurity.MaskPhoneNumber("+251911223344"));
    }

    [Fact]
    public void SecurityHelper_NormalizesReferencesFromReceiptUrls()
    {
        const string cbeUrl = "https://apps.cbe.com.et:4130/cbe/receipt/FT241234ABCD5678?ts=123";
        var norm = PaymentDestinationSecurity.NormalizeReference(cbeUrl);
        Assert.Equal("FT241234ABCD5678", norm);
    }
}
