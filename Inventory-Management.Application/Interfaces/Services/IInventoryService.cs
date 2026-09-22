using Inventory_Management.Application.DTOs.Common;
using Inventory_Management.Application.DTOs.Inventory;
using Inventory_Management.Domain.Enums;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IInventoryService
{
    Task IncreaseStockAsync(
        Guid productId,
        int quantity,
        InventoryTransactionType type,
        Guid? referenceId,
        string? referenceType,
        string? notes,
        string? createdBy);

    Task DecreaseStockAsync(
        Guid productId,
        int quantity,
        InventoryTransactionType type,
        Guid? referenceId,
        string? referenceType,
        string? notes,
        string? createdBy);

    Task AdjustStockAsync(
        Guid productId,
        int delta,
        string? notes,
        string? createdBy);

    Task<PagedResponse<InventoryTransactionDto>> GetTransactionsAsync(InventoryTransactionFilterDto filter);

    Task<ProductStockDto> GetProductStockAsync(Guid productId);
}
