using Inventory_Management.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Inventory_Management.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly string _bucketName;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public FileStorageService(Supabase.Client supabaseClient, IConfiguration config)
    {
        _supabaseClient = supabaseClient ?? throw new ArgumentNullException(nameof(supabaseClient));
        _bucketName = config["Supabase:Bucket"] ?? "products";
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

        var fileName = $"{Guid.NewGuid()}{ext}";
        
        using var stream = image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        await _supabaseClient.Storage.From(_bucketName).Upload(bytes, fileName, new Supabase.Storage.FileOptions { ContentType = image.ContentType });

        return _supabaseClient.Storage.From(_bucketName).GetPublicUrl(fileName);
    }

    public void DeleteProductImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        try
        {
            var fileName = GetFileNameFromUrl(imageUrl);
            
            if (!string.IsNullOrEmpty(fileName))
            {
                _supabaseClient.Storage.From(_bucketName).Remove(new List<string> { fileName }).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // Optionally log the error, but don't crash if file deletion fails
        }
    }

    private string? GetFileNameFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var lastSlashIndex = url.LastIndexOf('/');
        if (lastSlashIndex != -1 && lastSlashIndex < url.Length - 1)
        {
            var fileNameWithParams = url.Substring(lastSlashIndex + 1);
            var questionMarkIndex = fileNameWithParams.IndexOf('?');
            return questionMarkIndex != -1 ? fileNameWithParams.Substring(0, questionMarkIndex) : fileNameWithParams;
        }

        return null;
    }
}
