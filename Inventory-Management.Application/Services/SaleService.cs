using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.DTOs.SaleItem;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory_Management.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly IGenericRepository<PaymentVerification> _verificationRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ICurrentTenant _currentTenant;
    private readonly MediatR.IMediator _mediator;

    public SaleService(
        ISaleRepository saleRepository,
        IGenericRepository<Product> productRepository,
        IGenericRepository<Customer> customerRepository,
        IGenericRepository<PaymentVerification> verificationRepository,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IBackgroundTaskQueue taskQueue,
        ICurrentTenant currentTenant,
        MediatR.IMediator mediator)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _verificationRepository = verificationRepository;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _taskQueue = taskQueue;
        _currentTenant = currentTenant;
        _mediator = mediator;
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, string? userId, string? cashierName)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("A sale cannot be completed with an empty cart.");

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
                throw new InvalidOperationException($"Invalid item quantity: {item.Quantity}. Quantity must be greater than zero.");
            if (item.UnitPrice < 0)
                throw new InvalidOperationException($"Invalid unit price: {item.UnitPrice}. Price cannot be negative.");
        }

        Sale? created = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var productsDict = new Dictionary<Guid, Product>();
            foreach (var item in dto.Items)
            {
                if (!productsDict.ContainsKey(item.ProductId))
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId)
                        ?? throw new KeyNotFoundException($"Product with ID {item.ProductId} was not found.");
                    productsDict[item.ProductId] = product;
                }

                var prod = productsDict[item.ProductId];
                if (prod.QuantityInStock < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product '{prod.Name}'. Available: {prod.QuantityInStock}, Requested: {item.Quantity}.");
                }
            }

            string? customerName = dto.CustomerName;
            if (dto.CustomerId.HasValue)
            {
                var customer = await _customerRepository.GetByIdAsync(dto.CustomerId.Value);
                if (customer != null)
                    customerName = customer.Name;
            }

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                SaleNumber = await _saleRepository.GenerateUniqueSaleNumberAsync(),
                SaleDate = DateTime.UtcNow,
                CustomerId = dto.CustomerId,
                CustomerName = customerName,
                UserId = userId,
                CashierName = cashierName,
                PaymentMethod = (!string.IsNullOrWhiteSpace(dto.PaymentMethod) && dto.PaymentMethod.Equals("Mobile Banking", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(dto.BankName))
                    ? $"Mobile Banking ({dto.BankName.Trim()})"
                    : (string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod),
                Notes = dto.Notes,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };

            decimal subtotal = 0m;

            foreach (var itemDto in dto.Items)
            {
                var product = productsDict[itemDto.ProductId];

                decimal lineSubtotal = itemDto.Quantity * itemDto.UnitPrice;
                decimal lineDiscount = Math.Max(0m, itemDto.DiscountAmount);
                decimal lineTotal = Math.Max(0m, lineSubtotal - lineDiscount);

                await _inventoryService.DecreaseStockAsync(
                    product.Id,
                    itemDto.Quantity,
                    InventoryTransactionType.Sale,
                    sale.Id,
                    "Sale",
                    $"Sale {sale.SaleNumber}",
                    userId);

                subtotal += lineSubtotal;

                sale.SaleItems.Add(new SaleItem
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SKU = product.SKU,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    DiscountAmount = lineDiscount,
                    Subtotal = lineSubtotal,
                    TotalPrice = lineTotal,
                    IsBottleExchange = itemDto.IsBottleExchange,
                    BottleDepositAmount = itemDto.BottleDepositAmount
                });
            }

            sale.Subtotal = subtotal;
            sale.DiscountAmount = Math.Max(0m, dto.DiscountAmount);
            sale.TaxAmount = Math.Max(0m, dto.TaxAmount);
            sale.AdditionalBottleDeposit = Math.Max(0m, dto.AdditionalBottleDeposit);
            sale.TotalAmount = Math.Max(0m, subtotal - sale.DiscountAmount + sale.TaxAmount + sale.AdditionalBottleDeposit);
            sale.AmountReceived = dto.AmountReceived;
            sale.ChangeAmount = sale.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(0m, dto.AmountReceived - sale.TotalAmount)
                : 0m;

            if (dto.PaymentVerificationId.HasValue && dto.PaymentVerificationId.Value != Guid.Empty)
            {
                var verification = await _verificationRepository.GetByIdAsync(dto.PaymentVerificationId.Value)
                    ?? throw new KeyNotFoundException($"Payment verification with ID '{dto.PaymentVerificationId.Value}' was not found.");

                if (verification.Status != PaymentVerificationStatus.Verified && verification.IsManualReviewApproved != true)
                {
                    throw new InvalidOperationException($"Cannot link unverified payment transaction (status: {verification.Status}).");
                }

                if (verification.SaleId.HasValue && verification.SaleId.Value != Guid.Empty && verification.SaleId.Value != sale.Id)
                {
                    throw new InvalidOperationException("This verification record has already been associated with another sale.");
                }

                sale.PaymentVerificationId = verification.Id;
                sale.IsPaymentVerified = true;
                sale.PaymentVerificationStatus = "Verified";
                verification.SaleId = sale.Id;
                await _verificationRepository.UpdateAsync(verification);

                var bank = verification.PaymentDestination?.BankName ?? verification.PaymentDestination?.Name ?? verification.BankCode ?? verification.Provider;
                if (!string.IsNullOrWhiteSpace(bank))
                {
                    if (bank.Equals("Telebirr", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(verification.BankCode, "telebirr", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(verification.Provider, "telebirr", StringComparison.OrdinalIgnoreCase))
                    {
                        sale.PaymentMethod = "Telebirr";
                    }
                    else if (sale.PaymentMethod.StartsWith("Mobile Banking", StringComparison.OrdinalIgnoreCase) || sale.PaymentMethod == "Mobile Payment")
                    {
                        sale.PaymentMethod = $"Mobile Banking ({bank})";
                    }
                }
            }
            else if (dto.VerificationSkipped)
            {
                sale.IsPaymentVerified = false;
                sale.PaymentVerificationStatus = "Skipped";
                var auditNote = $"[Payment Verification Skipped by {cashierName ?? userId ?? "Cashier"} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]";
                sale.Notes = string.IsNullOrWhiteSpace(sale.Notes) ? auditNote : $"{sale.Notes}\n{auditNote}";
            }
            else
            {
                sale.IsPaymentVerified = false;
                sale.PaymentVerificationStatus = "Unverified";
            }

            await _saleRepository.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();
            created = sale;

            // Process bottle tracking
            var bottleCmd = new Inventory_Management.Application.Features.Sales.Commands.ProcessSaleBottlesCommand
            {
                SaleId = sale.Id,
                CustomerId = sale.CustomerId,
                User = cashierName ?? userId ?? "System",
                AdditionalBottleDeposit = sale.AdditionalBottleDeposit,
                Items = dto.Items.Select(x => new Inventory_Management.Application.Features.Sales.Commands.ProcessSaleBottleItem
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    IsBottleExchange = x.IsBottleExchange,
                    BottleDepositAmount = x.BottleDepositAmount
                }).ToList()
            };
            await _mediator.Send(bottleCmd);
        });

        if (created != null)
        {
            var notificationDto = new Inventory_Management.Application.DTOs.Notification.NotificationDto
            {
                Title = "Sale Completed",
                Message = $"Sale {created.SaleNumber} was completed successfully.",
                Type = "info",
                Icon = "receipt",
                RelatedEntityId = created.Id,
                RelatedEntityType = "Sale",
                Link = $"/sales-history/{created.Id}"
            };

            var tenantId = _currentTenant?.TenantId;
            await _taskQueue.QueueBackgroundWorkItemAsync(tenantId, async (sp, ct) =>
            {
                var ns = sp.GetRequiredService<INotificationService>();
                await ns.SendToRolesAsync(new[] { "Admin", "Manager" }, notificationDto, ct);
            });
        }

        return MapToDto(created!);
    }

    public async Task<PagedResponse<SaleDto>> GetPagedSalesAsync(SaleFilterDto filter, string? currentUserId, string? currentRole)
    {
        var pagedSales = await _saleRepository.GetPagedSalesAsync(filter, currentUserId, currentRole);
        return new PagedResponse<SaleDto>
        {
            Items = pagedSales.Items.Select(MapToDto).ToList(),
            TotalCount = pagedSales.TotalCount,
            Page = pagedSales.Page,
            PageSize = pagedSales.PageSize
        };
    }

    public async Task<SaleDto> GetByIdAsync(Guid id)
    {
        var sale = await _saleRepository.GetWithItemsByIdAsync(id)
            ?? throw new KeyNotFoundException($"Sale transaction with ID '{id}' was not found.");

        return MapToDto(sale);
    }

    public async Task CancelSaleAsync(Guid id, string? currentUserId, string? currentRole)
    {
        if (string.IsNullOrEmpty(currentRole) ||
            (!currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
             !currentRole.Equals("Manager", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Only Administrators and Managers can cancel sales.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sale = await _saleRepository.GetWithItemsByIdAsync(id)
                ?? throw new KeyNotFoundException($"Sale transaction with ID '{id}' was not found.");

            if (sale.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Sale '{sale.SaleNumber}' is already cancelled.");

            foreach (var item in sale.SaleItems)
            {
                await _inventoryService.IncreaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    InventoryTransactionType.SaleReturn,
                    sale.Id,
                    "SaleReturn",
                    $"Return of sale {sale.SaleNumber}",
                    currentUserId);
            }

            sale.Status = "Cancelled";
            await _saleRepository.UpdateAsync(sale);
            await _unitOfWork.SaveChangesAsync();
        });
    }

    private static SaleDto MapToDto(Sale s)
    {
        var pv = s.PaymentVerification;
        var bankName = pv?.PaymentDestination?.BankName ?? pv?.PaymentDestination?.Name ?? pv?.BankCode;
        var provider = pv?.Provider;

        var paymentMethod = s.PaymentMethod;

        if (paymentMethod.Equals("Telebirr", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "telebirr", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(bankName, "telebirr", StringComparison.OrdinalIgnoreCase))
        {
            paymentMethod = "Telebirr";
            if (string.IsNullOrWhiteSpace(bankName)) bankName = "Telebirr";
            if (string.IsNullOrWhiteSpace(provider)) provider = "telebirr";
        }
        else if (paymentMethod.StartsWith("Mobile Banking (", StringComparison.OrdinalIgnoreCase) && paymentMethod.EndsWith(")"))
        {
            if (string.IsNullOrWhiteSpace(bankName))
            {
                bankName = paymentMethod["Mobile Banking (".Length..^1].Trim();
            }
        }
        else if (paymentMethod.Equals("Mobile Banking", StringComparison.OrdinalIgnoreCase) ||
                 paymentMethod.Equals("Mobile Payment", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(bankName))
            {
                paymentMethod = $"Mobile Banking ({bankName})";
            }
            else
            {
                paymentMethod = "Mobile Banking";
            }
        }

        return new()
        {
            Id = s.Id,
            SaleNumber = s.SaleNumber,
            SaleDate = s.SaleDate,
            CustomerId = s.CustomerId,
            CustomerName = s.CustomerName,
            UserId = s.UserId,
            CashierName = s.CashierName,
            Subtotal = s.Subtotal,
            DiscountAmount = s.DiscountAmount,
            TaxAmount = s.TaxAmount,
            AdditionalBottleDeposit = s.AdditionalBottleDeposit,
            TotalBottleDeposit = s.SaleItems.Sum(si => si.BottleDepositAmount * si.Quantity),
            TotalAmount = s.TotalAmount,
            PaymentMethod = paymentMethod,
            BankName = bankName,
            PaymentProvider = provider,
            AmountReceived = s.AmountReceived,
            ChangeAmount = s.ChangeAmount,
            Notes = s.Notes,
            Status = s.Status,
            PaymentVerificationId = s.PaymentVerificationId,
            IsPaymentVerified = s.IsPaymentVerified,
            PaymentVerificationStatus = s.PaymentVerificationStatus,
            CreatedAt = s.CreatedAt,
            Items = s.SaleItems.Select(si => new SaleItemDto
            {
                Id = si.Id,
                SaleId = si.SaleId,
                ProductId = si.ProductId,
                ProductName = si.ProductName,
                SKU = si.SKU,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                DiscountAmount = si.DiscountAmount,
                Subtotal = si.Subtotal,
                TotalPrice = si.TotalPrice,
                IsBottleExchange = si.IsBottleExchange,
                BottleDepositAmount = si.BottleDepositAmount
            }).ToList()
        };
    }
}

