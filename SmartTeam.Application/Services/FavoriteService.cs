using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FavoriteService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<FavoriteDto> AddToFavoritesAsync(Guid userId, CreateFavoriteDto createFavoriteDto, UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var userExists = await _unitOfWork.Repository<User>()
            .AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        
        if (!userExists)
        {
            throw new ArgumentException("User not found or inactive.");
        }

        // Check if product exists and is active
        var product = await _unitOfWork.Repository<Product>()
            .FirstOrDefaultAsync(p => p.Id == createFavoriteDto.ProductId && p.IsActive, cancellationToken);
        
        if (product == null)
        {
            throw new ArgumentException("Product not found or inactive.");
        }

        // Check if already in favorites
        var existingFavorite = await _unitOfWork.Repository<UserFavorite>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == createFavoriteDto.ProductId, cancellationToken);
        
        if (existingFavorite != null)
        {
            throw new InvalidOperationException("Product is already in favorites.");
        }

        // Create new favorite
        var favorite = new UserFavorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = createFavoriteDto.ProductId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<UserFavorite>().AddAsync(favorite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get the favorite with product details
        var favoriteWithProduct = await _unitOfWork.Repository<UserFavorite>()
            .FirstOrDefaultWithIncludesAsync(f => f.Id == favorite.Id, f => f.Product, f => f.Product.Category);

        var favoriteDto = _mapper.Map<FavoriteDto>(favoriteWithProduct);
        
        if (favoriteDto.Product != null)
        {
            favoriteDto.Product.IsFavorite = true;
        }

        return favoriteDto;
    }

    public async Task<bool> RemoveFromFavoritesAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var favorite = await _unitOfWork.Repository<UserFavorite>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, cancellationToken);
        
        if (favorite == null)
        {
            return false;
        }

        _unitOfWork.Repository<UserFavorite>().Remove(favorite);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<FavoriteListDto> GetUserFavoritesAsync(Guid userId, int page = 1, int pageSize = 20, UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // Limit max page size

        // Get all user favorites with product details
        var allFavorites = await _unitOfWork.Repository<UserFavorite>()
            .FindAsync(f => f.UserId == userId, cancellationToken);

        var favoritesWithProducts = new List<UserFavorite>();
        
        foreach (var favorite in allFavorites)
        {
            var favoriteWithProduct = await _unitOfWork.Repository<UserFavorite>()
                .FirstOrDefaultWithIncludesAsync(
                    f => f.Id == favorite.Id, 
                    f => f.Product, 
                    f => f.Product.Category);
            
            if (favoriteWithProduct?.Product != null && favoriteWithProduct.Product.IsActive)
            {
                favoritesWithProducts.Add(favoriteWithProduct);
            }
        }

        // Sort by creation date (newest first)
        var sortedFavorites = favoritesWithProducts
            .OrderByDescending(f => f.CreatedAt)
            .ToList();

        // Get total count
        var totalCount = sortedFavorites.Count;

        // Apply pagination
        var pagedFavorites = sortedFavorites
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var favoriteDtos = _mapper.Map<List<FavoriteDto>>(pagedFavorites);

        foreach (var favoriteDto in favoriteDtos)
        {
            if (favoriteDto.Product != null)
            {
                favoriteDto.Product.IsFavorite = true;
            }
        }

        return new FavoriteListDto
        {
            Favorites = favoriteDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            HasNextPage = page * pageSize < totalCount,
            HasPreviousPage = page > 1
        };
    }

    public async Task<FavoriteStatusDto> GetFavoriteStatusAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var isFavorite = await _unitOfWork.Repository<UserFavorite>()
            .AnyAsync(f => f.UserId == userId && f.ProductId == productId, cancellationToken);

        return new FavoriteStatusDto
        {
            ProductId = productId,
            IsFavorite = isFavorite
        };
    }

    public async Task<BulkFavoriteStatusDto> GetBulkFavoriteStatusAsync(Guid userId, List<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var userFavorites = await _unitOfWork.Repository<UserFavorite>()
            .FindAsync(f => f.UserId == userId && productIds.Contains(f.ProductId), cancellationToken);

        var favoriteProductIds = userFavorites.Select(f => f.ProductId).ToHashSet();

        var favoriteStatuses = productIds.Select(productId => new FavoriteStatusDto
        {
            ProductId = productId,
            IsFavorite = favoriteProductIds.Contains(productId)
        }).ToList();

        return new BulkFavoriteStatusDto
        {
            FavoriteStatuses = favoriteStatuses
        };
    }

    public async Task<bool> ToggleFavoriteAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var existingFavorite = await _unitOfWork.Repository<UserFavorite>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, cancellationToken);

        if (existingFavorite != null)
        {
            // Remove from favorites
            _unitOfWork.Repository<UserFavorite>().Remove(existingFavorite);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false; // Removed from favorites
        }
        else
        {
            // Add to favorites
            try
            {
                var createDto = new CreateFavoriteDto { ProductId = productId };
                await AddToFavoritesAsync(userId, createDto, null, cancellationToken);
                return true; // Added to favorites
            }
            catch (ArgumentException)
            {
                // Product or user not found/inactive
                return false;
            }
            catch (InvalidOperationException)
            {
                // Already in favorites (shouldn't happen due to our check above, but just in case)
                return true;
            }
        }
    }

    public async Task<int> GetUserFavoritesCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Count only favorites with active products
        var favorites = await _unitOfWork.Repository<UserFavorite>()
            .FindAsync(f => f.UserId == userId, cancellationToken);

        var activeCount = 0;
        foreach (var favorite in favorites)
        {
            var product = await _unitOfWork.Repository<Product>()
                .FirstOrDefaultAsync(p => p.Id == favorite.ProductId && p.IsActive, cancellationToken);
            
            if (product != null)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    public async Task<bool> ClearUserFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userFavorites = await _unitOfWork.Repository<UserFavorite>()
            .FindAsync(f => f.UserId == userId, cancellationToken);

        if (!userFavorites.Any())
        {
            return false;
        }

        _unitOfWork.Repository<UserFavorite>().RemoveRange(userFavorites);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }


}
