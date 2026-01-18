using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class FilterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public FilterType Type { get; set; }
    public string TypeName => Type.ToString();
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<FilterOptionDto> Options { get; set; } = new();
}

public class FilterOptionDto
{
    public Guid Id { get; set; }
    public Guid FilterId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFilterDto
{
    public string Name { get; set; } = string.Empty;
    public FilterType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public List<CreateFilterOptionDto> Options { get; set; } = new();
}

public class CreateFilterOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

public class UpdateFilterDto
{
    public string Name { get; set; } = string.Empty;
    public FilterType Type { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateFilterOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class FilterSearchDto
{
    public string? SearchTerm { get; set; }
    public FilterType? Type { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "SortOrder";
    public string? SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedFilterResultDto
{
    public IEnumerable<FilterDto> Filters { get; set; } = new List<FilterDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class FilterStatisticsDto
{
    public int TotalFilters { get; set; }
    public int ActiveFilters { get; set; }
    public int InactiveFilters { get; set; }
    public Dictionary<FilterType, int> FiltersByType { get; set; } = new();
    public int TotalFilterOptions { get; set; }
    public int ProductsWithFilters { get; set; }
}

public class ProductAttributeValueDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid FilterId { get; set; }
    public string FilterName { get; set; } = string.Empty;
    public Guid? FilterOptionId { get; set; }
    public string? FilterOptionDisplayName { get; set; }
    public string? CustomValue { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignFilterToProductDto
{
    public Guid ProductId { get; set; }
    public Guid FilterId { get; set; }
    public Guid? FilterOptionId { get; set; }
    public string? CustomValue { get; set; }
}

public class BulkAssignFilterDto
{
    public List<Guid> ProductIds { get; set; } = new();
    public Guid FilterId { get; set; }
    public Guid? FilterOptionId { get; set; }
    public string? CustomValue { get; set; }
}

public class FilterTypeDto
{
    public FilterType Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ProductFilterCriteriaDto
{
    public Guid? CategoryId { get; set; }
    public string? BrandSlug { get; set; }
    public bool? IsHotDeal { get; set; }
    public bool? IsRecommended { get; set; }
    public string? SearchTerm { get; set; }
    public List<FilterCriteriaDto> FilterCriteria { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } = "Name"; // Name, Price, CreatedAt
    public string? SortOrder { get; set; } = "asc"; // asc, desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class FilterCriteriaDto
{
    public Guid FilterId { get; set; }
    public List<Guid> FilterOptionIds { get; set; } = new();
    public string? CustomValue { get; set; } // For text/range filters
    public decimal? MinValue { get; set; } // For range filters
    public decimal? MaxValue { get; set; } // For range filters
}

public class FilteredProductsResultDto
{
    public IEnumerable<ProductListDto> Products { get; set; } = new List<ProductListDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public IEnumerable<FilterDto> AppliedFilters { get; set; } = new List<FilterDto>();
}