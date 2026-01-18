using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class ImageCompressionTestController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IImageCompressionService _imageCompressionService;

    public ImageCompressionTestController(IFileUploadService fileUploadService, IImageCompressionService imageCompressionService)
    {
        _fileUploadService = fileUploadService;
        _imageCompressionService = imageCompressionService;
    }

    [HttpPost("test-compression")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> TestImageCompression(
        IFormFile imageFile,
        [FromForm] string folder = "products",
        [FromForm] int quality = 85,
        [FromForm] int maxWidth = 1200,
        [FromForm] int maxHeight = 1200)
    {
        try
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No image file provided");

            var originalSize = imageFile.Length;
            var originalFileName = imageFile.FileName;
            var originalExtension = Path.GetExtension(originalFileName);

            var compressedBytes = await _imageCompressionService.CompressImageAsync(
                imageFile, quality, maxWidth, maxHeight);

            var uploadedPath = await _fileUploadService.UploadFileAsync(imageFile, folder);

            var compressionRatio = Math.Round((1.0 - (double)compressedBytes.Length / originalSize) * 100, 2);
            var sizeReduction = originalSize - compressedBytes.Length;

            var result = new
            {
                Success = true,
                OriginalFile = new
                {
                    Name = originalFileName,
                    Size = originalSize,
                    SizeFormatted = FormatFileSize(originalSize),
                    Extension = originalExtension
                },
                CompressedFile = new
                {
                    Size = compressedBytes.Length,
                    SizeFormatted = FormatFileSize(compressedBytes.Length),
                    CompressionRatio = $"{compressionRatio}%",
                    SizeReduction = sizeReduction,
                    SizeReductionFormatted = FormatFileSize(sizeReduction)
                },
                Settings = new
                {
                    Quality = quality,
                    MaxWidth = maxWidth,
                    MaxHeight = maxHeight,
                    Folder = folder
                },
                UploadedPath = uploadedPath,
                Message = $"Image compressed successfully! Reduced by {compressionRatio}% ({FormatFileSize(sizeReduction)} saved)"
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Error = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    [HttpPost("test-all-folders")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> TestAllFolderTypes(IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return BadRequest("No image file provided");

        var originalSize = imageFile.Length;
        var results = new List<object>();

        var folders = new[]
        {
            new { Name = "products", Quality = 85, MaxWidth = 1200, MaxHeight = 1200 },
            new { Name = "categories", Quality = 80, MaxWidth = 800, MaxHeight = 600 },
            new { Name = "banners", Quality = 85, MaxWidth = 1920, MaxHeight = 1080 },
            new { Name = "brands", Quality = 80, MaxWidth = 400, MaxHeight = 400 }
        };

        foreach (var folder in folders)
        {
            try
            {
                imageFile.OpenReadStream().Position = 0;

                var compressedBytes = await _imageCompressionService.CompressImageAsync(
                    imageFile, folder.Quality, folder.MaxWidth, folder.MaxHeight);

                var compressionRatio = Math.Round((1.0 - (double)compressedBytes.Length / originalSize) * 100, 2);

                results.Add(new
                {
                    Folder = folder.Name,
                    Settings = folder,
                    CompressedSize = compressedBytes.Length,
                    CompressedSizeFormatted = FormatFileSize(compressedBytes.Length),
                    CompressionRatio = $"{compressionRatio}%",
                    SizeReduction = FormatFileSize(originalSize - compressedBytes.Length)
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    Folder = folder.Name,
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            OriginalSize = originalSize,
            OriginalSizeFormatted = FormatFileSize(originalSize),
            Results = results
        });
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}