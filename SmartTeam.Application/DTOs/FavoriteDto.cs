namespace SmartTeam.Application.DTOs;

public class FavoriteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public ProductListDto Product { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class CreateFavoriteDto
{
    public Guid ProductId { get; set; }
}

public class FavoriteListDto
{
    public IEnumerable<FavoriteDto> Favorites { get; set; } = new List<FavoriteDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class FavoriteStatusDto
{
    public Guid ProductId { get; set; }
    public bool IsFavorite { get; set; }
}

public class BulkFavoriteStatusDto
{
    public List<FavoriteStatusDto> FavoriteStatuses { get; set; } = new();
}
