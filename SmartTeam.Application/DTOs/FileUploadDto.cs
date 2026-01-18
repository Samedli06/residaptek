using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartTeam.Application.DTOs;

public class FileUploadDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    public string? Folder { get; set; } = "products";
}

public class MultipleFileUploadDto
{
    [Required]
    public IFormFileCollection Files { get; set; } = null!;
    
    public string? Folder { get; set; } = "products";
}

public class FileUploadResponseDto
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

public class MultipleFileUploadResponseDto
{
    public List<FileUploadResponseDto> Files { get; set; } = new();
    public int TotalFiles { get; set; }
    public string Message { get; set; } = string.Empty;
}

