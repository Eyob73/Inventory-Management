using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Inventory_Management.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveProductImageAsync(IFormFile image, string? oldImageUrl = null)
    {
        if (image == null || image.Length == 0)
            throw new ArgumentException("Image file is empty or missing.");

        if (image.Length > MaxFileSize)
            throw new ArgumentException("Image file exceeds the maximum allowed size of 5 MB.");

        var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            throw new ArgumentException("Invalid image format. Allowed formats are: JPG, JPEG, PNG, WEBP.");

        var fileName = $"{Guid.NewGuid()}{ext}";
        var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadFolder = Path.Combine(webRootPath, "uploads", "products");

        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        var filePath = Path.Combine(uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        if (!string.IsNullOrEmpty(oldImageUrl))
        {
            DeleteProductImage(oldImageUrl);
        }

        return $"/uploads/products/{fileName}";
    }

    public void DeleteProductImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        // Ensure we only process relative URLs inside the expected folder
        if (!imageUrl.StartsWith("/uploads/products/")) return;

        var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(webRootPath, "uploads", "products", fileName);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Optionally log the error, but don't crash if file deletion fails
            }
        }
    }
}
