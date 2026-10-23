using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Application.Common;
using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.DTOs.SaleItem;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Application.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using MediatR;
using Moq;
using Xunit;

namespace Inventory_Management.Tests;

public class SaleServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepoMock;
    private readonly Mock<IGenericRepository<Product>> _productRepoMock;
    private readonly Mock<IGenericRepository<Customer>> _customerRepoMock;
    private readonly Mock<IGenericRepository<PaymentVerification>> _verifRepoMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IBackgroundTaskQueue> _taskQueueMock;
    private readonly Mock<ICurrentTenant> _currentTenantMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SaleService _service;

    public SaleServiceTests()
    {
        _saleRepoMock = new Mock<ISaleRepository>();
        _productRepoMock = new Mock<IGenericRepository<Product>>();
        _customerRepoMock = new Mock<IGenericRepository<Customer>>();
        _verifRepoMock = new Mock<IGenericRepository<PaymentVerification>>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationServiceMock = new Mock<INotificationService>();
        _taskQueueMock = new Mock<IBackgroundTaskQueue>();
        _currentTenantMock = new Mock<ICurrentTenant>();
        _mediatorMock = new Mock<IMediator>();

        _unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((op, ct) => op());

        _service = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _verifRepoMock.Object,
            _inventoryServiceMock.Object,
            _unitOfWorkMock.Object,
            _notificationServiceMock.Object,
            _taskQueueMock.Object,
            _currentTenantMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task CreateSaleAsync_ExcludesAdditionalBottleDepositFromTotalAmount()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Draft Beer",
            SKU = "BEER-01",
            Price = 100m,
            QuantityInStock = 50,
            IsReturnable = true
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _saleRepoMock.Setup(r => r.GenerateUniqueSaleNumberAsync())
            .ReturnsAsync("SAL-20261023-0001");

        Sale? capturedSale = null;
        _saleRepoMock.Setup(r => r.AddAsync(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSale = s)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new CreateSaleDto
        {
            PaymentMethod = "Cash",
            DiscountAmount = 10m,
            TaxAmount = 15m,
            AdditionalBottleDeposit = 50m,
            AmountReceived = 185m, // subtotal(100) - discount(10) + tax(15) + bottleDeposit(30) + additionalDeposit(50) = 185
            Items = new List<CreateSaleItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 100m,
                    DiscountAmount = 0m,
                    IsBottleExchange = false,
                    BottleDepositAmount = 30m
                }
            }
        };

        // Act
        var result = await _service.CreateSaleAsync(dto, "user-1", "Cashier 1");

        // Assert
        Assert.NotNull(capturedSale);
        // TotalAmount should be subtotal(100) - discount(10) + tax(15) = 105.
        // It must NOT include the 50 ETB additional deposit or 30 ETB bottle deposit.
        Assert.Equal(105m, capturedSale.TotalAmount);
        Assert.Equal(30m, capturedSale.TotalBottleDeposit);
        Assert.Equal(50m, capturedSale.AdditionalBottleDeposit);
        // Total customer payable is 105 + 30 + 50 = 185, so change amount is 0.
        Assert.Equal(0m, capturedSale.ChangeAmount);

        // Result DTO should also reflect this
        Assert.Equal(105m, result.TotalAmount);
        Assert.Equal(30m, result.TotalBottleDeposit);
        Assert.Equal(50m, result.AdditionalBottleDeposit);
        Assert.Equal(0m, result.ChangeAmount);
    }

    [Fact]
    public async Task CreateSaleAsync_CalculatesCashChangeCorrectly_WhenCustomerPaysExcess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Soft Drink",
            SKU = "SODA-01",
            Price = 50m,
            QuantityInStock = 20
        };

        _productRepoMock.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _saleRepoMock.Setup(r => r.GenerateUniqueSaleNumberAsync())
            .ReturnsAsync("SAL-20261023-0002");

        Sale? capturedSale = null;
        _saleRepoMock.Setup(r => r.AddAsync(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSale = s)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new CreateSaleDto
        {
            PaymentMethod = "Cash",
            DiscountAmount = 0m,
            TaxAmount = 0m,
            AdditionalBottleDeposit = 25m,
            AmountReceived = 200m, // Customer gives 200 ETB cash for 50 + 20(deposit) + 25(additional) = 95 ETB
            Items = new List<CreateSaleItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 50m,
                    DiscountAmount = 0m,
                    IsBottleExchange = false,
                    BottleDepositAmount = 20m
                }
            }
        };

        // Act
        var result = await _service.CreateSaleAsync(dto, "user-1", "Cashier 1");

        // Assert
        Assert.NotNull(capturedSale);
        Assert.Equal(50m, capturedSale.TotalAmount);
        Assert.Equal(20m, capturedSale.TotalBottleDeposit);
        Assert.Equal(25m, capturedSale.AdditionalBottleDeposit);
        // Total payable is 50 + 20 + 25 = 95. Change is 200 - 95 = 105.
        Assert.Equal(105m, capturedSale.ChangeAmount);
        Assert.Equal(105m, result.ChangeAmount);
    }
}
