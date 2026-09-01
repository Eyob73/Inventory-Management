using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Sale;
using Inventory_Management.Application.DTOs.SaleItem;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;

    public SaleService(
        ISaleRepository saleRepository,
        IGenericRepository<Product> productRepository,
        IGenericRepository<Customer> customerRepository,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
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
                PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod,
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
                    TotalPrice = lineTotal
                });
            }

            sale.Subtotal = subtotal;
            sale.DiscountAmount = Math.Max(0m, dto.DiscountAmount);
            sale.TaxAmount = Math.Max(0m, dto.TaxAmount);
            sale.TotalAmount = Math.Max(0m, subtotal - sale.DiscountAmount + sale.TaxAmount);
            sale.AmountReceived = dto.AmountReceived;
            sale.ChangeAmount = sale.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(0m, dto.AmountReceived - sale.TotalAmount)
                : 0m;

            await _saleRepository.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();
            created = sale;
        });

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

    private static SaleDto MapToDto(Sale s) => new()
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
        TotalAmount = s.TotalAmount,
        PaymentMethod = s.PaymentMethod,
        AmountReceived = s.AmountReceived,
        ChangeAmount = s.ChangeAmount,
        Notes = s.Notes,
        Status = s.Status,
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
            TotalPrice = si.TotalPrice
        }).ToList()
    };
}
