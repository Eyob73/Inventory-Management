using Microsoft.AspNetCore.Http;

namespace Inventory_Management.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<string> SaveProductImageAsync(IFormFile image, string? oldImageUrl = null);
    void DeleteProductImage(string imageUrl);
}
