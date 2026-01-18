using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Services;

public interface IFavoriteService
{
    Task<FavoriteDto> AddToFavoritesAsync(Guid userId, CreateFavoriteDto createFavoriteDto, UserRole? userRole = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<FavoriteListDto> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 20, UserRole? userRole = null, CancellationToken cancellationToken = default);
    Task<FavoriteStatusDto> GetFavoriteStatusAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<BulkFavoriteStatusDto> GetBulkFavoriteStatusAsync(Guid userId, List<Guid> productIds, CancellationToken cancellationToken = default);
    Task<bool> ToggleFavoriteAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<int> GetUserFavoritesCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ClearUserFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);
}
