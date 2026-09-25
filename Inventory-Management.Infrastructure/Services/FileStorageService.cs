using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Inventory_Management.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public FileStorageService(IConfiguration config)
    {
        // Read from appsettings if available, fallback to hardcoded for simplicity
        var cloudName = config["Cloudinary:CloudName"] ?? "rnlfawky";
        var apiKey = config["Cloudinary:ApiKey"] ?? "368673597326217";
        var apiSecret = config["Cloudinary:ApiSecret"] ?? "7T3NWgGXFnDkpavuTzZoKqO0vu4";

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
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

        if (!string.IsNullOrEmpty(oldImageUrl))
        {
            DeleteProductImage(oldImageUrl);
        }

        using var stream = image.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(image.FileName, stream),
            Folder = "products",
            // You can optionally add transformations here, e.g. resizing
            // Transformation = new Transformation().Width(800).Height(800).Crop("limit")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Image upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }

    public void DeleteProductImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        try
        {
            var publicId = GetPublicIdFromUrl(imageUrl);
            
            if (!string.IsNullOrEmpty(publicId))
            {
                var deletionParams = new DeletionParams(publicId);
                _cloudinary.Destroy(deletionParams);
            }
        }
        catch
        {
            // Optionally log the error, but don't crash if file deletion fails
        }
    }

    private string? GetPublicIdFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        // Try to find the folder if it exists in the URL to extract the public ID
        var folderPrefix = "products/";
        var folderIndex = url.IndexOf(folderPrefix);

        if (folderIndex != -1)
        {
            var publicIdWithExt = url.Substring(folderIndex);
            var dotIndex = publicIdWithExt.LastIndexOf('.');
            return dotIndex != -1 ? publicIdWithExt.Substring(0, dotIndex) : publicIdWithExt;
        }

        return null;
    }
}
