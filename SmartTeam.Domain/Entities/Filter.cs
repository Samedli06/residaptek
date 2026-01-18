namespace SmartTeam.Domain.Entities;

public class Filter
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public FilterType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<FilterOption> Options { get; set; } = new List<FilterOption>();
    public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();
}

public class FilterOption
{
    public Guid Id { get; set; }
    public Guid FilterId { get; set; }
    public Filter Filter { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; } // For color filters
    public string? IconUrl { get; set; } // For icon-based filters
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();
}

public class ProductAttributeValue
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid FilterId { get; set; }
    public Filter Filter { get; set; } = null!;
    public Guid? FilterOptionId { get; set; }
    public FilterOption? FilterOption { get; set; }
    public string? CustomValue { get; set; } // For text/number filters
    public DateTime CreatedAt { get; set; }
}

public enum FilterType
{
    Select = 0,      // Single selection (Brand, Size, etc.)
    MultiSelect = 1, // Multiple selection (Features, Tags, etc.)
    Range = 2,       // Price range, Rating range, etc.
    Text = 3,        // Search text
    Color = 4        // Color picker
}
