using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Application.Common;
using Inventory_Management.Application.DTOs.Payment;
using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.DTOs.SaleItem;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Application.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Inventory_Management.Tests;

public class PaymentVerificationServiceTests
{
    private readonly Mock<IGenericRepository<PaymentVerification>> _verifRepoMock;
    private readonly Mock<IGenericRepository<PaymentDestination>> _destRepoMock;
    private readonly Mock<ISaleRepository> _saleRepositoryMock;
    private readonly Mock<IGenericRepository<Product>> _productRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IChekiVerificationService> _chekiServiceMock;
    private readonly Mock<ILogger<PaymentVerificationService>> _loggerMock;
    private readonly PaymentVerificationService _service;

    private readonly Guid _testTenantId = Guid.NewGuid();
    private readonly Guid _testSaleId = Guid.NewGuid();
    private readonly Guid _testDestId = Guid.NewGuid();

    public PaymentVerificationServiceTests()
    {
        _verifRepoMock = new Mock<IGenericRepository<PaymentVerification>>();
        _destRepoMock = new Mock<IGenericRepository<PaymentDestination>>();
        _saleRepositoryMock = new Mock<ISaleRepository>();
        _productRepoMock = new Mock<IGenericRepository<Product>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _chekiServiceMock = new Mock<IChekiVerificationService>();
        _loggerMock = new Mock<ILogger<PaymentVerificationService>>();

        _service = new PaymentVerificationService(
            _verifRepoMock.Object,
            _destRepoMock.Object,
            _saleRepositoryMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            _chekiServiceMock.Object,
            _loggerMock.Object);
    }

    private Sale CreateTestSale(decimal totalAmount = 1500m, decimal amountReceived = 0m)
    {
        return new Sale
        {
            Id = _testSaleId,
            TenantId = _testTenantId,
            SaleNumber = "SAL-001",
            TotalAmount = totalAmount,
            AmountReceived = amountReceived,
            PaymentMethod = "BankTransfer",
            Status = "Pending"
        };
    }

    private PaymentDestination CreateTestDestination(PaymentDestinationType type = PaymentDestinationType.BankAccount)
    {
        return new PaymentDestination
        {
            Id = _testDestId,
            TenantId = _testTenantId,
            Name = "Primary CBE",
            Type = type,
            BankCode = "cbe",
            BankName = "Commercial Bank of Ethiopia",
            AccountNumber = "1000123456789",
            PhoneNumber = "+251911223344",
            IsActive = true,
            IsDefault = true
        };
    }

    [Fact]
    public async Task VerifyCustomerPayment_SuccessfulVerification_ConfirmsSaleAndCreatesVerifiedRecord()
    {
        // Arrange
        var sale = CreateTestSale(totalAmount: 1500m, amountReceived: 0m);
        var dest = CreateTestDestination();

        _saleRepositoryMock.Setup(r => r.Query())
            .Returns(new List<Sale> { sale }.AsAsyncQueryable());

        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());

        var emptyVerifications = new List<PaymentVerification>().AsAsyncQueryable();
        _verifRepoMock.Setup(r => r.Query()).Returns(emptyVerifications);

        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = true,
            Amount = 1500m,
            Currency = "ETB",
            ReceiverAccount = "1000****6789",
            ReceiverName = "Primary CBE Account",
            Date = DateTime.UtcNow.ToString("o")
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            SaleId = _testSaleId,
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT2400112233"
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.Verified, result.Status);
        Assert.Equal(DestinationMatchStatus.Matched, result.DestinationMatchStatus);
        Assert.Equal(1500m, result.VerifiedAmount);
        Assert.True(result.SaleUpdated);
        Assert.Equal("Completed", sale.Status);
        Assert.Equal(1500m, sale.AmountReceived);

        _verifRepoMock.Verify(r => r.AddAsync(It.IsAny<PaymentVerification>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task VerifyCustomerPayment_AmountExceeding500Margin_PreventsSaleConfirmation()
    {
        // Arrange: Sale is 1500, but Cheki verified amount is 2100 (exceeds 1500 + 500 margin)
        var sale = CreateTestSale(totalAmount: 1500m, amountReceived: 0m);
        var dest = CreateTestDestination();

        _saleRepositoryMock.Setup(r => r.Query())
            .Returns(new List<Sale> { sale }.AsAsyncQueryable());
        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());

        _verifRepoMock.Setup(r => r.Query()).Returns(new List<PaymentVerification>().AsAsyncQueryable());

        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = true,
            Amount = 2100m,
            Currency = "ETB",
            ReceiverAccount = "1000****6789"
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            SaleId = _testSaleId,
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT_EXCEEDS_2100"
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.AmountMismatch, result.Status);
        Assert.Equal("AMOUNT_EXCEEDS_BALANCE", result.FailureCode);
        Assert.False(result.SaleUpdated);
        Assert.NotEqual("Completed", sale.Status);
    }

    [Fact]
    public async Task VerifyCustomerPayment_AmountWithin500MarginAbove_IsVerified()
    {
        // Arrange: Sale is 1500, Cheki verified amount is 1800 (within 1500 + 500 margin)
        var sale = CreateTestSale(totalAmount: 1500m, amountReceived: 0m);
        var dest = CreateTestDestination();

        _saleRepositoryMock.Setup(r => r.Query())
            .Returns(new List<Sale> { sale }.AsAsyncQueryable());
        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());

        _verifRepoMock.Setup(r => r.Query()).Returns(new List<PaymentVerification>().AsAsyncQueryable());

        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = true,
            Amount = 1800m,
            Currency = "ETB",
            ReceiverAccount = "1000****6789",
            ReceiverName = "Primary CBE Account",
            Date = DateTime.UtcNow.ToString("o")
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            SaleId = _testSaleId,
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT_WITHIN_MARGIN_1800"
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.Verified, result.Status);
        Assert.Equal(1800m, result.VerifiedAmount);
        Assert.True(result.SaleUpdated);
        Assert.Equal("Completed", sale.Status);
    }

    [Fact]
    public async Task VerifyCustomerPayment_CartUnderpayment_TriggersAmountMismatch()
    {
        // Arrange: Cart total is 1000, Cheki verified is 800 (underpayment, no below margin allowed)
        var dest = CreateTestDestination();
        var product = new Product { Id = Guid.NewGuid(), Name = "Beer", Price = 500m, SKU = "B-1" };

        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());
        _verifRepoMock.Setup(r => r.Query()).Returns(new List<PaymentVerification>().AsAsyncQueryable());
        _productRepoMock.Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = true,
            Amount = 800m, // Under 1000m cart total
            Currency = "ETB",
            ReceiverAccount = "1000****6789"
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT_UNDERPAY_800",
            Cart = new CreateSaleDto
            {
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, Quantity = 2, UnitPrice = 500m }
                }
            }
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.AmountMismatch, result.Status);
        Assert.Equal("AMOUNT_UNDERPAYMENT", result.FailureCode);
    }

    [Fact]
    public async Task VerifyCustomerPayment_DestinationMismatch_PreventsSaleConfirmation()
    {
        // Arrange: Verified receiver account is completely different from registered destination
        var sale = CreateTestSale(totalAmount: 1500m, amountReceived: 0m);
        var dest = CreateTestDestination();

        _saleRepositoryMock.Setup(r => r.Query())
            .Returns(new List<Sale> { sale }.AsAsyncQueryable());
        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());

        _verifRepoMock.Setup(r => r.Query()).Returns(new List<PaymentVerification>().AsAsyncQueryable());

        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = true,
            Amount = 1500m,
            Currency = "ETB",
            ReceiverAccount = "1000999999999" // Mismatched!
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            SaleId = _testSaleId,
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT_WRONG_DEST"
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.DestinationMismatch, result.Status);
        Assert.False(result.SaleUpdated);
        Assert.NotEqual("Completed", sale.Status);
    }

    [Fact]
    public async Task VerifyCustomerPayment_UnableToConfirmDestination_FlagsManualReviewAndPreventsConfirmation()
    {
        // Arrange: Verified response has missing receiver account info
        var sale = CreateTestSale(totalAmount: 1500m, amountReceived: 0m);
        var dest = CreateTestDestination();

        _saleRepositoryMock.Setup(r => r.Query())
            .Returns(new List<Sale> { sale }.AsAsyncQueryable());
        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());

        _verifRepoMock.Setup(r => r.Query()).Returns(new List<PaymentVerification>().AsAsyncQueryable());

        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = true,
            Amount = 1500m,
            Currency = "ETB",
            ReceiverAccount = null // Missing receiver account!
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            SaleId = _testSaleId,
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT_MASKED_UNKNOWN"
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.UnableToConfirmDestination, result.Status);
        Assert.Equal(DestinationMatchStatus.UnableToConfirm, result.DestinationMatchStatus);
        Assert.True(result.NeedsManualReview);
        Assert.False(result.SaleUpdated);
        Assert.NotEqual("Completed", sale.Status);
    }

    [Fact]
    public async Task VerifyCustomerPayment_ChekiRejected_MarksVerificationAsRejected()
    {
        // Arrange: Cheki returned verified: false
        var sale = CreateTestSale(totalAmount: 1500m);
        var dest = CreateTestDestination();

        _saleRepositoryMock.Setup(r => r.Query())
            .Returns(new List<Sale> { sale }.AsAsyncQueryable());
        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());

        _verifRepoMock.Setup(r => r.Query()).Returns(new List<PaymentVerification>().AsAsyncQueryable());

        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = false,
            Reason = "Transaction not found on CBE network"
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            SaleId = _testSaleId,
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT_NOT_EXIST"
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.Rejected, result.Status);
        Assert.False(result.SaleUpdated);
        Assert.NotEqual("Completed", sale.Status);
    }

    [Fact]
    public async Task VerifyCustomerPayment_PreSaleWithCart_AuthoritativelyRecalculatesAmountAndSucceeds()
    {
        // Arrange
        var dest = CreateTestDestination();
        var prodId = Guid.NewGuid();
        var product = new Product
        {
            Id = prodId,
            Name = "Draft Beer Keg",
            SKU = "BEER-01",
            Price = 250m,
            QuantityInStock = 50
        };

        _destRepoMock.Setup(r => r.Query())
            .Returns(new List<PaymentDestination> { dest }.AsAsyncQueryable());

        _productRepoMock.Setup(r => r.GetByIdAsync(prodId))
            .ReturnsAsync(product);

        _verifRepoMock.Setup(r => r.Query()).Returns(new List<PaymentVerification>().AsAsyncQueryable());

        // 4 items * 250 = 1000, discount 100, tax 50 = 950 ETB
        var chekiResponse = new ChekiVerifyResponse
        {
            Success = true,
            Verified = true,
            Amount = 950m,
            Currency = "ETB",
            ReceiverAccount = "1000****6789",
            ReceiverName = "Primary CBE Account",
            Date = DateTime.UtcNow.ToString("o")
        };

        _chekiServiceMock.Setup(c => c.VerifyPaymentAsync(It.IsAny<ChekiVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chekiResponse);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new VerifyPaymentRequestDto
        {
            PaymentDestinationId = _testDestId,
            SubmittedReference = "FT_PRESALE_123",
            Cart = new Inventory_Management.Application.DTOs.Sale.CreateSaleDto
            {
                DiscountAmount = 100m,
                TaxAmount = 50m,
                Items = new List<Inventory_Management.Application.DTOs.SaleItem.CreateSaleItemDto>
                {
                    new()
                    {
                        ProductId = prodId,
                        Quantity = 4,
                        UnitPrice = 9999m // Intentional untrusted frontend price
                    }
                }
            }
        };

        // Act
        var result = await _service.VerifyCustomerPaymentAsync(request, "user-1", "Cashier John");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentVerificationStatus.Verified, result.Status);
        Assert.Null(result.SaleId);
        Assert.False(result.SaleUpdated);
        Assert.Equal(950m, result.ExpectedAmount); // Authoritative calculation: 4*250 - 100 + 50 = 950
        Assert.Equal(950m, result.VerifiedAmount);

        _verifRepoMock.Verify(r => r.AddAsync(It.IsAny<PaymentVerification>()), Times.Once);
    }
}
