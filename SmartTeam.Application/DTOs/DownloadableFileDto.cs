using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartTeam.Application.DTOs;

public class DownloadableFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public int DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public string? UpdatedByUserName { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileSizeFormatted { get; set; } = string.Empty;
}

public class CreateDownloadableFileDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    [MaxLength(255)]
    public string? CustomFileName { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class UpdateDownloadableFileDto
{
    [MaxLength(255)]
    public string? CustomFileName { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public bool? IsActive { get; set; }
}

public class DownloadableFileSearchDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? ContentType { get; set; }
    public long? MinFileSize { get; set; }
    public long? MaxFileSize { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedDownloadableFileResultDto
{
    public IEnumerable<DownloadableFileDto> Files { get; set; } = new List<DownloadableFileDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public class DownloadableFileStatisticsDto
{
    public int TotalFiles { get; set; }
    public int ActiveFiles { get; set; }
    public int InactiveFiles { get; set; }
    public long TotalFileSize { get; set; }
    public string TotalFileSizeFormatted { get; set; } = string.Empty;
    public int TotalDownloads { get; set; }
    public Dictionary<string, int> FilesByCategory { get; set; } = new();
    public Dictionary<string, int> FilesByContentType { get; set; } = new();
    public List<DownloadableFileDto> MostDownloadedFiles { get; set; } = new();
    public List<DownloadableFileDto> RecentFiles { get; set; } = new();
}

public class FileDownloadResponseDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
}
