using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class BannerDto
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
    public BannerType Type { get; set; }
    public string TypeName => Type.ToString();
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsCurrentlyActive => IsActive && 
        (StartDate == null || StartDate <= DateTime.UtcNow) && 
        (EndDate == null || EndDate >= DateTime.UtcNow);
}

public class CreateBannerDto
{
    public string Title { get; set; } = string.Empty;
    public bool TitleVisible { get; set; } = true;
    public string? Description { get; set; }
    public bool DescriptionVisible { get; set; } = true;
    public string? LinkUrl { get; set; }
    public string? ButtonText { get; set; }
    public bool ButtonVisible { get; set; } = true;
    public BannerType Type { get; set; } = BannerType.Hero;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CreateBannerWithImageDto
{
    public string Title { get; set; } = string.Empty;
    public bool TitleVisible { get; set; } = true;
    public string? Description { get; set; }
    public bool DescriptionVisible { get; set; } = true;
    public string? LinkUrl { get; set; }
    public string? ButtonText { get; set; }
    public bool ButtonVisible { get; set; } = true;
    public BannerType Type { get; set; } = BannerType.Hero;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateBannerDto
{
    public string Title { get; set; } = string.Empty;
    public bool TitleVisible { get; set; } = true;
    public string? Description { get; set; }
    public bool DescriptionVisible { get; set; } = true;
    public string? LinkUrl { get; set; }
    public string? ButtonText { get; set; }
    public bool ButtonVisible { get; set; } = true;
    public BannerType Type { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class BannerSearchDto
{
    public string? SearchTerm { get; set; }
    public BannerType? Type { get; set; } = BannerType.Hero; // Always Hero
    public bool? IsActive { get; set; }
    public bool? IsCurrentlyActive { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public string? SortBy { get; set; } = "SortOrder";
    public string? SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedBannerResultDto
{
    public IEnumerable<BannerDto> Banners { get; set; } = new List<BannerDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class BannerStatisticsDto
{
    public int TotalBanners { get; set; }
    public int ActiveBanners { get; set; }
    public int InactiveBanners { get; set; }
    public int CurrentlyActiveBanners { get; set; }
    public int ScheduledBanners { get; set; }
    public int ExpiredBanners { get; set; }
}

public class BannerTypeDto
{
    public BannerType Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

