namespace SmartTeam.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string Sku { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsHotDeal { get; set; } = false;
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; } // Main product image URL
    public string? DetailImageUrl { get; set; } // Detail page image URL
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Category relationship
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    // Brand relationship
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    
    // Pricing
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    
    // Images
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    
    // Attributes and filters
    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
    
    // Specifications
    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
    
    // Navigation properties
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
}

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? MediumUrl { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; } = false;
    public bool IsDetailImage { get; set; } = false; // Flag to identify detail images
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
