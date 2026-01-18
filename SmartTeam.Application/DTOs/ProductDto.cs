using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string Sku { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsHotDeal { get; set; }
    public int StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string? ParentCategoryName { get; set; }
    public string? ParentCategorySlug { get; set; }
    public string? SubCategoryName { get; set; }
    public string? SubCategorySlug { get; set; }
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public string? ImageUrl { get; set; }
    public string? DetailImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public bool IsFavorite { get; set; } = false;
    public DateTime CreatedAt { get; set; }
}

public class ProductListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string Sku { get; set; } = string.Empty;
    public bool IsHotDeal { get; set; }
    public int StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string? ParentCategoryName { get; set; }
    public string? ParentCategorySlug { get; set; }
    public string? SubCategoryName { get; set; }
    public string? SubCategorySlug { get; set; }
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public bool IsFavorite { get; set; } = false;
    public List<ProductFilterDto> Filters { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? MediumUrl { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string Sku { get; set; } = string.Empty;
    public bool IsHotDeal { get; set; }
    public int StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
}

public class CreateProductWithImageDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string Sku { get; set; } = string.Empty;
    public bool IsHotDeal { get; set; }
    public int StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string Sku { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsHotDeal { get; set; }
    public int StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public List<string>? DetailImageUrls { get; set; } // For tracking existing detail images
}

public enum StockStatus
{
    OutOfStock,
    LowStock,
    InStock
}

// ProductWithAllPricesDto removed as it is no longer needed with unified pricing.

public class ProductStockDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public StockStatus StockStatus { get; set; }
    public string StockStatusText { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecommendedProductsDto
{
    public IEnumerable<ProductListDto> BasedOnFavorites { get; set; } = new List<ProductListDto>();
    public IEnumerable<ProductListDto> BasedOnCategory { get; set; } = new List<ProductListDto>();
    public IEnumerable<ProductListDto> HotDeals { get; set; } = new List<ProductListDto>();
    public IEnumerable<ProductListDto> RecentlyAdded { get; set; } = new List<ProductListDto>();
    public IEnumerable<ProductListDto> SimilarProducts { get; set; } = new List<ProductListDto>();
}

public class RecommendationRequestDto
{
    public Guid? ProductId { get; set; } // For similar products
    public Guid? CategoryId { get; set; } // For category-based recommendations
    public int? Limit { get; set; } = 10; // Number of recommendations per category
}

public class StockSummaryDto
{
    public int TotalProducts { get; set; }
    public int InStockProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int InactiveProducts { get; set; }
}

public class ProductSpecificationDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public List<SpecificationGroupDto> SpecificationGroups { get; set; } = new();
}

public class SpecificationGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public List<SpecificationItemDto> Items { get; set; } = new();
}

public class SpecificationItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public SpecificationType Type { get; set; } = SpecificationType.Text;
}

public class CreateProductSpecificationDto
{
    public Guid ProductId { get; set; }
    public List<CreateSpecificationGroupDto> SpecificationGroups { get; set; } = new();
}

public class UpdateProductSpecificationDto
{
    public List<UpdateSpecificationGroupDto> SpecificationGroups { get; set; } = new();
}

public class CreateSpecificationGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public List<CreateSpecificationItemDto> Items { get; set; } = new();
}

public class UpdateSpecificationGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public List<UpdateSpecificationItemDto> Items { get; set; } = new();
}

public class CreateSpecificationItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public SpecificationType Type { get; set; } = SpecificationType.Text;
}

public class UpdateSpecificationItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public SpecificationType Type { get; set; } = SpecificationType.Text;
}

public class ProductFilterDto
{
    public Guid FilterId { get; set; }
    public string FilterName { get; set; } = string.Empty;
    public FilterType FilterType { get; set; }
    public Guid? FilterOptionId { get; set; }
    public string? FilterOptionValue { get; set; }
    public string? FilterOptionDisplayName { get; set; }
    public string? CustomValue { get; set; }
    public string? Color { get; set; }
    public string? IconUrl { get; set; }
}

public class GlobalSearchResultDto
{
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public IEnumerable<BrandDto> Brands { get; set; } = new List<BrandDto>();
    public IEnumerable<ProductListDto> Products { get; set; } = new List<ProductListDto>();
}
