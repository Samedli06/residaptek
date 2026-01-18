using Microsoft.AspNetCore.Http;

namespace SmartTeam.Application.Services;

public interface IImageCompressionService
{
    Task<byte[]> CompressImageAsync(IFormFile file, int quality = 85, int maxWidth = 1920, int maxHeight = 1080);
    Task<byte[]> CompressImageAsync(Stream imageStream, string format, int quality = 85, int maxWidth = 1920, int maxHeight = 1080);
    string GetFileExtension(string format);
    string GetMimeType(string format);
}