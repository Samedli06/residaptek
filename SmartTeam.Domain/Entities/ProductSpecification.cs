namespace SmartTeam.Domain.Entities;

public class ProductSpecification
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public SpecificationType Type { get; set; } = SpecificationType.Text;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}

public enum SpecificationType
{
    Text = 0,
    Color = 1,
    Size = 2,
    Technical = 3,
    Feature = 4
}
