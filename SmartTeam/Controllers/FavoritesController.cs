using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize] // All endpoints require authentication
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    /// <summary>
    /// Add a product to user's favorites
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FavoriteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FavoriteDto>> AddToFavorites([FromBody] CreateFavoriteDto createFavoriteDto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User not authenticated.");
            }

            var userRole = GetCurrentUserRole();
            var favorite = await _favoriteService.AddToFavoritesAsync(userId.Value, createFavoriteDto, userRole, cancellationToken);
            
            return CreatedAtAction(nameof(GetFavoriteStatus), new { productId = createFavoriteDto.ProductId }, favorite);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Remove a product from user's favorites
    /// </summary>
    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromFavorites(Guid productId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        var removed = await _favoriteService.RemoveFromFavoritesAsync(userId.Value, productId, cancellationToken);
        
        if (!removed)
        {
            return NotFound("Product not found in favorites.");
        }

        return NoContent();
    }

    /// <summary>
    /// Get user's favorites with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FavoriteListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FavoriteListDto>> GetUserFavorites(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        var userRole = GetCurrentUserRole();
        var favorites = await _favoriteService.GetUserFavoritesAsync(userId.Value, page, pageSize, userRole, cancellationToken);
        
        return Ok(favorites);
    }

    /// <summary>
    /// Check if a specific product is in user's favorites
    /// </summary>
    [HttpGet("status/{productId:guid}")]
    [ProducesResponseType(typeof(FavoriteStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FavoriteStatusDto>> GetFavoriteStatus(Guid productId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        var status = await _favoriteService.GetFavoriteStatusAsync(userId.Value, productId, cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Check favorite status for multiple products at once
    /// </summary>
    [HttpPost("bulk-status")]
    [ProducesResponseType(typeof(BulkFavoriteStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BulkFavoriteStatusDto>> GetBulkFavoriteStatus([FromBody] List<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds == null || !productIds.Any())
        {
            return BadRequest("Product IDs are required.");
        }

        if (productIds.Count > 100)
        {
            return BadRequest("Maximum 100 product IDs allowed per request.");
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        var bulkStatus = await _favoriteService.GetBulkFavoriteStatusAsync(userId.Value, productIds, cancellationToken);
        return Ok(bulkStatus);
    }

    /// <summary>
    /// Toggle favorite status for a product (add if not favorite, remove if favorite)
    /// </summary>
    [HttpPost("toggle/{productId:guid}")]
    [ProducesResponseType(typeof(FavoriteStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FavoriteStatusDto>> ToggleFavorite(Guid productId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        try
        {
            var isNowFavorite = await _favoriteService.ToggleFavoriteAsync(userId.Value, productId, cancellationToken);
            
            return Ok(new FavoriteStatusDto
            {
                ProductId = productId,
                IsFavorite = isNowFavorite
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get the count of user's favorites
    /// </summary>
    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> GetFavoritesCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        var count = await _favoriteService.GetUserFavoritesCountAsync(userId.Value, cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Clear all user's favorites
    /// </summary>
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearFavorites(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("User not authenticated.");
        }

        var cleared = await _favoriteService.ClearUserFavoritesAsync(userId.Value, cancellationToken);
        
        if (!cleared)
        {
            return NotFound("No favorites found to clear.");
        }

        return NoContent();
    }

    private UserRole? GetCurrentUserRole()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var roleString = User.FindFirst(ClaimTypes.Role)?.Value;
            if (Enum.TryParse<UserRole>(roleString, out var role))
            {
                return role;
            }
        }
        
        return UserRole.NormalUser;
    }

    private Guid? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
        }
        
        return null;
    }
}
