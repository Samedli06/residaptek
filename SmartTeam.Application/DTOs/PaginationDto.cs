using System.ComponentModel.DataAnnotations;

namespace SmartTeam.Application.DTOs;

/// <summary>
/// Generic pagination request parameters
/// </summary>
public class PaginationRequestDto
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order (asc or desc)
    /// </summary>
    public string SortOrder { get; set; } = "asc";
}

/// <summary>
/// Generic paginated result
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Number of items in the current page
    /// </summary>
    public int Count => Items.Count();

    /// <summary>
    /// Starting index of items in the current page (1-based)
    /// </summary>
    public int StartIndex => (Page - 1) * PageSize + 1;

    /// <summary>
    /// Ending index of items in the current page (1-based)
    /// </summary>
    public int EndIndex => Math.Min(StartIndex + Count - 1, TotalCount);
}

/// <summary>
/// Product-specific pagination request
/// </summary>
public class ProductPaginationRequestDto : PaginationRequestDto
{
    /// <summary>
    /// Filter by category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Filter by brand slug
    /// </summary>
    public string? BrandSlug { get; set; }

    /// <summary>
    /// Filter by hot deals only
    /// </summary>
    public bool? IsHotDeal { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Search term for product name/description
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Product-specific sort options
    /// </summary>
    public ProductSortOption ProductSortBy { get; set; } = ProductSortOption.Name;
}

/// <summary>
/// Available sort options for products
/// </summary>
public enum ProductSortOption
{
    Name,
    Price,
    CreatedAt,
    StockQuantity,
    CategoryName,
    BrandName
}

/// <summary>
/// Brand-specific pagination request
/// </summary>
public class BrandPaginationRequestDto : PaginationRequestDto
{
    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Search term for brand name
    /// </summary>
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Search-specific pagination request
/// </summary>
public class SearchPaginationRequestDto : PaginationRequestDto
{
    /// <summary>
    /// Search term (required)
    /// </summary>
    [Required(ErrorMessage = "Search term is required")]
    [MinLength(2, ErrorMessage = "Search term must be at least 2 characters")]
    [MaxLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Filter by category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Filter by brand slug
    /// </summary>
    public string? BrandSlug { get; set; }

    /// <summary>
    /// Filter by hot deals only
    /// </summary>
    public bool? IsHotDeal { get; set; }

    /// <summary>
    /// Product-specific sort options
    /// </summary>
    public ProductSortOption ProductSortBy { get; set; } = ProductSortOption.Name;
}

/// <summary>
/// Hot deals pagination request
/// </summary>
public class HotDealsPaginationRequestDto : PaginationRequestDto
{
    /// <summary>
    /// Filter by category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Filter by brand slug
    /// </summary>
    public string? BrandSlug { get; set; }

    /// <summary>
    /// Product-specific sort options
    /// </summary>
    public ProductSortOption ProductSortBy { get; set; } = ProductSortOption.Name;
}

/// <summary>
/// Recommended products pagination request
/// </summary>
public class RecommendedProductsPaginationRequestDto : PaginationRequestDto
{
    /// <summary>
    /// Product ID for similar products
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Category ID for category-based recommendations
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Filter by brand slug
    /// </summary>
    public string? BrandSlug { get; set; }

    /// <summary>
    /// Number of recommendations per category
    /// </summary>
    [Range(1, 50, ErrorMessage = "Limit must be between 1 and 50")]
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Product-specific sort options
    /// </summary>
    public ProductSortOption ProductSortBy { get; set; } = ProductSortOption.Name;
}
