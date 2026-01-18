using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartTeam.Application.DTOs;

public class ProductPdfDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
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

public class CreateProductPdfDto
{
    [Required]
    public Guid ProductId { get; set; }
    
    [Required]
    public IFormFile PdfFile { get; set; } = null!;
    
    [MaxLength(255)]
    public string? CustomFileName { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class UpdateProductPdfDto
{
    [MaxLength(255)]
    public string? CustomFileName { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool? IsActive { get; set; }
}

public class ProductPdfSearchDto
{
    public string? SearchTerm { get; set; }
    public Guid? ProductId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public long? MinFileSize { get; set; }
    public long? MaxFileSize { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedProductPdfResultDto
{
    public IEnumerable<ProductPdfDto> Files { get; set; } = new List<ProductPdfDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public class ProductPdfDownloadResponseDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
}
