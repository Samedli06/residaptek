namespace SmartTeam.Domain.Entities;

public class Banner
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool TitleVisible { get; set; } = true;
    public string? Description { get; set; }
    public bool DescriptionVisible { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? ButtonText { get; set; }
    public bool ButtonVisible { get; set; } = true;
    public BannerType Type { get; set; } = BannerType.Hero;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum BannerType
{
    Hero = 0        // Main hero banner
}
