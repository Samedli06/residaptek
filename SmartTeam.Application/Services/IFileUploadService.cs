using Microsoft.AspNetCore.Http;

namespace SmartTeam.Application.Services;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, string folder = "products");
    Task<bool> DeleteFileAsync(string filePath);
    Task<List<string>> UploadMultipleFilesAsync(IFormFileCollection files, string folder = "products");
    bool IsValidImageFile(IFormFile file);
    string GetFileUrl(string fileName, string folder = "products");
}

