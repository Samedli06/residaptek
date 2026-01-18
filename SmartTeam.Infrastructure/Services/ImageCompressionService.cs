using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SmartTeam.Application.Services;

namespace SmartTeam.Infrastructure.Services;

public class ImageCompressionService : IImageCompressionService
{
    private readonly ILogger<ImageCompressionService> _logger;

    public ImageCompressionService(ILogger<ImageCompressionService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> CompressImageAsync(IFormFile file, int quality = 85, int maxWidth = 1920, int maxHeight = 1080)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is null or empty");

        using var stream = file.OpenReadStream();
        var originalExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var outputFormat = GetOutputFormat(originalExtension);
        
        return await CompressImageAsync(stream, outputFormat, quality, maxWidth, maxHeight);
    }

    public async Task<byte[]> CompressImageAsync(Stream imageStream, string format, int quality = 85, int maxWidth = 1920, int maxHeight = 1080)
    {
        if (imageStream == null)
            throw new ArgumentException("Image stream is null");

        try
        {
            using var image = await Image.LoadAsync(imageStream);
            
            var (newWidth, newHeight) = CalculateNewDimensions(image.Width, image.Height, maxWidth, maxHeight);
            
            if (newWidth != image.Width || newHeight != image.Height)
            {
                image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
            }

            IImageEncoder encoder = format.ToLowerInvariant() switch
            {
                "jpeg" or "jpg" => new JpegEncoder { Quality = quality },
                "png" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
                "webp" => new WebpEncoder { Quality = quality },
                _ => new JpegEncoder { Quality = quality }
            };

            using var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, encoder);
            
            _logger.LogInformation($"Image compressed: {imageStream.Length} -> {outputStream.Length} bytes");
            
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image");
            throw new InvalidOperationException("Failed to compress image", ex);
        }
    }

    public string GetFileExtension(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg"
        };
    }

    public string GetMimeType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private static string GetOutputFormat(string originalExtension)
    {
        return originalExtension switch
        {
            ".png" => "png",
            ".webp" => "webp",
            _ => "jpeg"
        };
    }

    private static (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        if (maxWidth <= 0 && maxHeight <= 0)
            return (originalWidth, originalHeight);

        if ((maxWidth <= 0 || originalWidth <= maxWidth) && (maxHeight <= 0 || originalHeight <= maxHeight))
            return (originalWidth, originalHeight);

        double widthScale = maxWidth > 0 ? (double)maxWidth / originalWidth : double.MaxValue;
        double heightScale = maxHeight > 0 ? (double)maxHeight / originalHeight : double.MaxValue;
        double scale = Math.Min(widthScale, heightScale);

        return ((int)(originalWidth * scale), (int)(originalHeight * scale));
    }
}