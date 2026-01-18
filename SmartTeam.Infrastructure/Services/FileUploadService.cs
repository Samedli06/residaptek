using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartTeam.Application.Services;

namespace SmartTeam.Infrastructure.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IImageCompressionService _imageCompressionService;
    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly long _maxFileSize = 50 * 1024 * 1024; // 50MB for general files
    private readonly long _maxImageSize = 5 * 1024 * 1024; // 5MB for images

    public FileUploadService(IWebHostEnvironment webHostEnvironment, ILogger<FileUploadService> logger, IImageCompressionService imageCompressionService)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
        _imageCompressionService = imageCompressionService;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder = "products")
    {
        try
        {
            // For downloads folder, allow any file type
            // For product-pdfs folder, allow only PDF files
            // Otherwise validate as image
            if (folder == "downloads")
            {
                if (!IsValidFile(file))
                {
                    throw new ArgumentException("Invalid file or size");
                }
            }
            else if (folder == "product-pdfs")
            {
                if (!IsValidPdfFile(file))
                {
                    throw new ArgumentException("Invalid PDF file or size");
                }
            }
            else
            {
                if (!IsValidImageFile(file))
                {
                    throw new ArgumentException("Invalid file format or size");
                }
            }

            // Create upload directory if it doesn't exist
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (folder != "downloads" && folder != "product-pdfs" && IsValidImageFile(file))
            {
                await SaveCompressedImageAsync(file, filePath);
            }
            else
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            _logger.LogInformation($"File uploaded successfully: {fileName}");

            // Return relative path for database storage
            return $"/uploads/{folder}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            throw;
        }
    }

    public async Task<List<string>> UploadMultipleFilesAsync(IFormFileCollection files, string folder = "products")
    {
        var uploadedFiles = new List<string>();

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var filePath = await UploadFileAsync(file, folder);
                uploadedFiles.Add(filePath);
            }
        }

        return uploadedFiles;
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("DeleteFileAsync: filePath is null or empty");
                return false;
            }

            // Convert relative path to physical path
            var normalizedPath = filePath.TrimStart('/');
            var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

            _logger.LogInformation($"DeleteFileAsync: Attempting to delete - Original path: {filePath}, Physical path: {physicalPath}, WebRootPath: {_webHostEnvironment.WebRootPath}");

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _logger.LogInformation($"File deleted successfully: {filePath} -> {physicalPath}");
                return true;
            }
            else
            {
                _logger.LogWarning($"File does not exist: {physicalPath}");
                // Check if directory exists
                var directory = Path.GetDirectoryName(physicalPath);
                if (directory != null && Directory.Exists(directory))
                {
                    _logger.LogInformation($"Directory exists but file not found: {directory}");
                    // List files in directory for debugging
                    try
                    {
                        var filesInDir = Directory.GetFiles(directory);
                        _logger.LogInformation($"Files in directory ({directory}): {string.Join(", ", filesInDir.Select(Path.GetFileName))}");
                    }
                    catch { }
                }
                else
                {
                    _logger.LogWarning($"Directory does not exist: {directory}");
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file: {filePath}");
            return false;
        }
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > _maxImageSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedImageExtensions.Contains(extension))
            return false;

        // Check MIME type
        var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        return true;
    }

    public bool IsValidFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > _maxFileSize)
            return false;

        // Block potentially dangerous file types
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blockedExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".msi", ".dll" };
        
        if (blockedExtensions.Contains(extension))
            return false;

        return true;
    }

    public bool IsValidPdfFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > _maxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf")
            return false;

        // Check MIME type
        if (file.ContentType.ToLowerInvariant() != "application/pdf")
            return false;

        return true;
    }

    public string GetFileUrl(string fileName, string folder = "products")
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        return $"/uploads/{folder}/{fileName}";
    }

    private async Task SaveCompressedImageAsync(IFormFile file, string filePath)
    {
        try
        {
            var (quality, maxWidth, maxHeight) = GetCompressionSettings(filePath);
            var compressedImageBytes = await _imageCompressionService.CompressImageAsync(file, quality, maxWidth, maxHeight);
            await File.WriteAllBytesAsync(filePath, compressedImageBytes);
            
            _logger.LogInformation($"Image compressed: {file.Length} -> {compressedImageBytes.Length} bytes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error compressing image: {Path.GetFileName(filePath)}");
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }
    }

    private static (int quality, int maxWidth, int maxHeight) GetCompressionSettings(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath)?.Split(Path.DirectorySeparatorChar).LastOrDefault()?.ToLowerInvariant();
        
        return folder switch
        {
            "banners" => (85, 1920, 1080),
            "categories" => (80, 800, 600),
            "products" => (85, 1200, 1200),
            "brands" => (80, 400, 400),
            _ => (85, 1200, 1200)
        };
    }
}

