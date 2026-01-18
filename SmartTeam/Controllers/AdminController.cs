using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IProductService _productService;
    private readonly IFilterService _filterService;
    private readonly IBannerService _bannerService;
    private readonly IDownloadableFileService _downloadableFileService;
    private readonly IProductPdfService _productPdfService;
    private readonly IBrandService _brandService;
    private readonly IEmailService _emailService;

    public AdminController(IUserService userService, IAuthService authService, IProductService productService, IFilterService filterService, IBannerService bannerService, IDownloadableFileService downloadableFileService, IProductPdfService productPdfService, IBrandService brandService, IEmailService emailService)
    {
        _userService = userService;
        _authService = authService;
        _productService = productService;
        _filterService = filterService;
        _bannerService = bannerService;
        _downloadableFileService = downloadableFileService;
        _productPdfService = productPdfService;
        _brandService = brandService;
        _emailService = emailService;
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Search and filter users with pagination (Admin only)
    /// </summary>
    [HttpGet("users/search")]
    [ProducesResponseType(typeof(PagedUserResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedUserResultDto>> SearchUsers(
        [FromQuery] string? searchTerm,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchDto = new UserSearchDto
            {
                SearchTerm = searchTerm,
                Role = role,
                IsActive = isActive,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _userService.SearchUsersAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Update user information (Admin only)
    /// </summary>
    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(id, updateUserDto, cancellationToken);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update user role (Admin only)
    /// </summary>
    [HttpPut("users/{id:guid}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleDto updateUserRoleDto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateUserRoleAsync(id, updateUserRoleDto, cancellationToken);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deactivate user (Admin only)
    /// </summary>
    [HttpPost("users/{id:guid}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.DeactivateUserAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound("User not found");
            }

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Activate user (Admin only)
    /// </summary>
    [HttpPost("users/{id:guid}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ActivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.ActivateUserAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound("User not found");
        }

        return Ok(new { message = "User activated successfully" });
    }

    /// <summary>
    /// Delete user (Admin only)
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound("User not found");
            }

            return Ok(new { message = "User deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occurred while deleting the user: {ex.Message}");
        }
    }

    /// <summary>
    /// Get user statistics (Admin only)
    /// </summary>
    [HttpGet("users/statistics")]
    [ProducesResponseType(typeof(UserStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(cancellationToken);
        var usersList = users.ToList();

        var statistics = new UserStatisticsDto
        {
            TotalUsers = usersList.Count,
            AdminUsers = usersList.Count(u => u.Role == UserRole.Admin),
            NormalUsers = usersList.Count(u => u.Role == UserRole.NormalUser)
        };

        return Ok(statistics);
    }

    /// <summary>
    /// Get all available user roles (Admin only)
    /// </summary>
    [HttpGet("users/roles")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IEnumerable<object>> GetUserRoles()
    {
        var roles = Enum.GetValues<UserRole>()
            .Select(role => new
            {
                value = (int)role,
                name = role.ToString()
            });

        return Ok(roles);
    }

    /// <summary>
    /// Get product stock status (Admin only)
    /// </summary>
    [HttpGet("products/stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductStockDto>>> GetProductStockStatus(CancellationToken cancellationToken)
    {
        var stockStatus = await _productService.GetProductStockStatusAsync(cancellationToken);
        return Ok(stockStatus);
    }

    /// <summary>
    /// Get stock summary statistics (Admin only)
    /// </summary>
    [HttpGet("products/stock/summary")]
    [ProducesResponseType(typeof(StockSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StockSummaryDto>> GetStockSummary(CancellationToken cancellationToken)
    {
        var summary = await _productService.GetStockSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    #region Filter Management

    /// <summary>
    /// Get all filters (Admin only)
    /// </summary>
    [HttpGet("filters")]
    [ProducesResponseType(typeof(IEnumerable<FilterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FilterDto>>> GetAllFilters(CancellationToken cancellationToken)
    {
        var filters = await _filterService.GetAllFiltersAsync(cancellationToken);
        return Ok(filters);
    }

    /// <summary>
    /// Search and filter filters with pagination (Admin only)
    /// </summary>
    [HttpGet("filters/search")]
    [ProducesResponseType(typeof(PagedFilterResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedFilterResultDto>> SearchFilters(
        [FromQuery] string? searchTerm,
        [FromQuery] FilterType? type,
        [FromQuery] bool? isActive,
        [FromQuery] string? sortBy = "SortOrder",
        [FromQuery] string? sortOrder = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchDto = new FilterSearchDto
            {
                SearchTerm = searchTerm,
                Type = type,
                IsActive = isActive,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _filterService.SearchFiltersAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get filter by ID (Admin only)
    /// </summary>
    [HttpGet("filters/{id:guid}")]
    [ProducesResponseType(typeof(FilterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FilterDto>> GetFilter(Guid id, CancellationToken cancellationToken)
    {
        var filter = await _filterService.GetFilterByIdAsync(id, cancellationToken);
        if (filter == null)
        {
            return NotFound();
        }

        return Ok(filter);
    }

    /// <summary>
    /// Get filter by slug (Admin only)
    /// </summary>
    [HttpGet("filters/slug/{slug}")]
    [ProducesResponseType(typeof(FilterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FilterDto>> GetFilterBySlug(string slug, CancellationToken cancellationToken)
    {
        var filter = await _filterService.GetFilterBySlugAsync(slug, cancellationToken);
        if (filter == null)
        {
            return NotFound();
        }

        return Ok(filter);
    }

    /// <summary>
    /// Create a new filter (Admin only)
    /// </summary>
    [HttpPost("filters")]
    [ProducesResponseType(typeof(FilterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FilterDto>> CreateFilter([FromBody] CreateFilterDto createFilterDto, CancellationToken cancellationToken)
    {
        try
        {
            var filter = await _filterService.CreateFilterAsync(createFilterDto, cancellationToken);
            return CreatedAtAction(nameof(GetFilter), new { id = filter.Id }, filter);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing filter (Admin only)
    /// </summary>
    [HttpPut("filters/{id:guid}")]
    [ProducesResponseType(typeof(FilterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FilterDto>> UpdateFilter(Guid id, [FromBody] UpdateFilterDto updateFilterDto, CancellationToken cancellationToken)
    {
        try
        {
            var filter = await _filterService.UpdateFilterAsync(id, updateFilterDto, cancellationToken);
            return Ok(filter);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete a filter (Admin only)
    /// </summary>
    [HttpDelete("filters/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteFilter(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _filterService.DeleteFilterAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound("Filter not found");
            }

            return Ok(new { message = "Filter deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reorder filters (Admin only)
    /// </summary>
    [HttpPut("filters/reorder")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ReorderFilters([FromBody] List<Guid> filterIds, CancellationToken cancellationToken)
    {
        try
        {
            await _filterService.ReorderFiltersAsync(filterIds, cancellationToken);
            return Ok(new { message = "Filters reordered successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Add filter option to a filter (Admin only)
    /// </summary>
    [HttpPost("filters/{filterId:guid}/options")]
    [ProducesResponseType(typeof(FilterOptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FilterOptionDto>> AddFilterOption(Guid filterId, [FromBody] CreateFilterOptionDto createOptionDto, CancellationToken cancellationToken)
    {
        try
        {
            var option = await _filterService.AddFilterOptionAsync(filterId, createOptionDto, cancellationToken);
            return CreatedAtAction(nameof(GetFilter), new { id = filterId }, option);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update filter option (Admin only)
    /// </summary>
    [HttpPut("filters/{filterId:guid}/options/{optionId:guid}")]
    [ProducesResponseType(typeof(FilterOptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FilterOptionDto>> UpdateFilterOption(Guid filterId, Guid optionId, [FromBody] UpdateFilterOptionDto updateOptionDto, CancellationToken cancellationToken)
    {
        try
        {
            var option = await _filterService.UpdateFilterOptionAsync(filterId, optionId, updateOptionDto, cancellationToken);
            return Ok(option);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete filter option (Admin only)
    /// </summary>
    [HttpDelete("filters/{filterId:guid}/options/{optionId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteFilterOption(Guid filterId, Guid optionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _filterService.DeleteFilterOptionAsync(filterId, optionId, cancellationToken);
            if (!result)
            {
                return NotFound("Filter option not found");
            }

            return Ok(new { message = "Filter option deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reorder filter options (Admin only)
    /// </summary>
    [HttpPut("filters/{filterId:guid}/options/reorder")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ReorderFilterOptions(Guid filterId, [FromBody] List<Guid> optionIds, CancellationToken cancellationToken)
    {
        try
        {
            await _filterService.ReorderFilterOptionsAsync(filterId, optionIds, cancellationToken);
            return Ok(new { message = "Filter options reordered successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get filter statistics (Admin only)
    /// </summary>
    [HttpGet("filters/statistics")]
    [ProducesResponseType(typeof(FilterStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FilterStatisticsDto>> GetFilterStatistics(CancellationToken cancellationToken)
    {
        var statistics = await _filterService.GetFilterStatisticsAsync(cancellationToken);
        return Ok(statistics);
    }

    /// <summary>
    /// Get available filter types (Admin only)
    /// </summary>
    [HttpGet("filters/types")]
    [ProducesResponseType(typeof(IEnumerable<FilterTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FilterTypeDto>>> GetFilterTypes()
    {
        var filterTypes = await _filterService.GetFilterTypesAsync();
        return Ok(filterTypes);
    }

    /// <summary>
    /// Assign filter to product (Admin only)
    /// </summary>
    [HttpPost("products/filters/assign")]
    [ProducesResponseType(typeof(ProductAttributeValueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductAttributeValueDto>> AssignFilterToProduct([FromBody] AssignFilterToProductDto assignDto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _filterService.AssignFilterToProductAsync(assignDto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Bulk assign filter to multiple products (Admin only)
    /// </summary>
    [HttpPost("products/filters/bulk-assign")]
    [ProducesResponseType(typeof(IEnumerable<ProductAttributeValueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductAttributeValueDto>>> BulkAssignFilterToProducts([FromBody] BulkAssignFilterDto bulkAssignDto, CancellationToken cancellationToken)
    {
        try
        {
            var results = await _filterService.BulkAssignFilterToProductsAsync(bulkAssignDto, cancellationToken);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get product attributes (filters assigned to a product) (Admin only)
    /// </summary>
    [HttpGet("products/{productId:guid}/filters")]
    [ProducesResponseType(typeof(IEnumerable<ProductAttributeValueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductAttributeValueDto>>> GetProductAttributes(Guid productId, CancellationToken cancellationToken)
    {
        var attributes = await _filterService.GetProductAttributesAsync(productId, cancellationToken);
        return Ok(attributes);
    }

    /// <summary>
    /// Remove filter from product (Admin only)
    /// </summary>
    [HttpDelete("products/{productId:guid}/filters/{filterId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> RemoveFilterFromProduct(Guid productId, Guid filterId, CancellationToken cancellationToken)
    {
        var result = await _filterService.RemoveFilterFromProductAsync(productId, filterId, cancellationToken);
        if (!result)
        {
            return NotFound("Filter assignment not found");
        }

        return Ok(new { message = "Filter removed from product successfully" });
    }

    /// <summary>
    /// Remove all filters from product (Admin only)
    /// </summary>
    [HttpDelete("products/{productId:guid}/filters")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> RemoveAllFiltersFromProduct(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _filterService.RemoveAllFiltersFromProductAsync(productId, cancellationToken);
        if (!result)
        {
            return NotFound("No filters found for this product");
        }

        return Ok(new { message = "All filters removed from product successfully" });
    }

    /// <summary>
    /// Get available filters for a category (Admin only)
    /// </summary>
    [HttpGet("categories/{categoryId:guid}/filters")]
    [ProducesResponseType(typeof(IEnumerable<FilterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FilterDto>>> GetAvailableFiltersForCategory(Guid categoryId, CancellationToken cancellationToken)
    {
        var filters = await _filterService.GetAvailableFiltersForCategoryAsync(categoryId, cancellationToken);
        return Ok(filters);
    }

    #endregion

    #region Banner Management

    /// <summary>
    /// Get all banners (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("banners")]
    [ProducesResponseType(typeof(IEnumerable<BannerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BannerDto>>> GetAllBanners(CancellationToken cancellationToken)
    {
        var banners = await _bannerService.GetAllBannersAsync(cancellationToken);
        
        // Add debugging information for image URLs
        foreach (var banner in banners)
        {
            if (!string.IsNullOrEmpty(banner.ImageUrl))
            {
                // Log the image URL for debugging
                Console.WriteLine($"Banner {banner.Id}: ImageUrl = {banner.ImageUrl}");
            }
        }
        
        return Ok(banners);
    }

    /// <summary>
    /// Search and filter banners with pagination (Admin only)
    /// </summary>
    [HttpGet("banners/search")]
    [ProducesResponseType(typeof(PagedBannerResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedBannerResultDto>> SearchBanners(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isCurrentlyActive,
        [FromQuery] DateTime? startDateFrom,
        [FromQuery] DateTime? startDateTo,
        [FromQuery] string? sortBy = "SortOrder",
        [FromQuery] string? sortOrder = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchDto = new BannerSearchDto
            {
                SearchTerm = searchTerm,
                Type = BannerType.Hero, // Always Hero since it's the only type
                IsActive = isActive,
                IsCurrentlyActive = isCurrentlyActive,
                StartDateFrom = startDateFrom,
                StartDateTo = startDateTo,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _bannerService.SearchBannersAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get banner by ID (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("banners/{id:guid}")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BannerDto>> GetBanner(Guid id, CancellationToken cancellationToken)
    {
        var banner = await _bannerService.GetBannerByIdAsync(id, cancellationToken);
        if (banner == null)
        {
            return NotFound();
        }

        return Ok(banner);
    }

    /// <summary>
    /// Create a new banner (Admin only)
    /// </summary>
    [HttpPost("banners")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BannerDto>> CreateBanner([FromBody] CreateBannerDto createBannerDto, CancellationToken cancellationToken)
    {
        try
        {
            var banner = await _bannerService.CreateBannerAsync(createBannerDto, cancellationToken);
            return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, banner);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Create a new banner with image upload (Admin only)
    /// </summary>
    [HttpPost("banners/with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BannerDto>> CreateBannerWithImage(
        [FromForm] string? bannerData,
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bannerData))
            {
                return BadRequest("bannerData field is required and must contain valid JSON");
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("imageFile is required");
            }

            CreateBannerWithImageDto createBannerDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                createBannerDto = System.Text.Json.JsonSerializer.Deserialize<CreateBannerWithImageDto>(bannerData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest($"Invalid JSON format for bannerData: {ex.Message}");
            }

            if (createBannerDto == null)
            {
                return BadRequest("Failed to parse banner data");
            }

            var banner = await _bannerService.CreateBannerWithImageAsync(createBannerDto, imageFile, cancellationToken);
            return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, banner);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Create a new banner with both desktop and mobile images (Admin only)
    /// </summary>
    [HttpPost("banners/with-images")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BannerDto>> CreateBannerWithImages(
        [FromForm] string? bannerData,
        IFormFile? imageFile,
        IFormFile? mobileImageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bannerData))
            {
                return BadRequest("bannerData field is required and must contain valid JSON");
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("imageFile is required");
            }

            CreateBannerWithImageDto createBannerDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                createBannerDto = System.Text.Json.JsonSerializer.Deserialize<CreateBannerWithImageDto>(bannerData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest($"Invalid JSON format for bannerData: {ex.Message}");
            }

            if (createBannerDto == null)
            {
                return BadRequest("Failed to parse banner data");
            }

            var banner = await _bannerService.CreateBannerWithImagesAsync(createBannerDto, imageFile, mobileImageFile, cancellationToken);
            return CreatedAtAction(nameof(GetBanner), new { id = banner.Id }, banner);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing banner (Admin only)
    /// </summary>
    [HttpPut("banners/{id:guid}")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BannerDto>> UpdateBanner(Guid id, [FromBody] UpdateBannerDto updateBannerDto, CancellationToken cancellationToken)
    {
        try
        {
            var banner = await _bannerService.UpdateBannerAsync(id, updateBannerDto, cancellationToken);
            return Ok(banner);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a banner (Admin only)
    /// </summary>
    [HttpDelete("banners/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteBanner(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bannerService.DeleteBannerAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound("Banner not found");
        }

        return Ok(new { message = "Banner deleted successfully" });
    }

    /// <summary>
    /// Upload banner main image (Admin only)
    /// </summary>
    [HttpPost("banners/{id:guid}/upload-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BannerDto>> UploadBannerImage(
        Guid id, 
        IFormFile imageFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var banner = await _bannerService.UploadBannerImageAsync(id, imageFile, cancellationToken);
            return Ok(banner);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Upload banner mobile image (Admin only)
    /// </summary>
    [HttpPost("banners/{id:guid}/upload-mobile-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BannerDto>> UploadBannerMobileImage(
        Guid id, 
        IFormFile imageFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var banner = await _bannerService.UploadBannerMobileImageAsync(id, imageFile, cancellationToken);
            return Ok(banner);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    /// <summary>
    /// Delete banner image (Admin only)
    /// </summary>
    [HttpDelete("banners/{id:guid}/delete-image")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteBannerImage(
        Guid id, 
        [FromQuery] string imageType = "main",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _bannerService.DeleteBannerImageAsync(id, imageType, cancellationToken);
            if (deleted)
            {
                return Ok(new { message = $"{imageType} image deleted successfully" });
            }
            return NotFound("Image not found");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reorder banners (Admin only)
    /// </summary>
    [HttpPut("banners/reorder")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ReorderBanners([FromBody] List<Guid> bannerIds, CancellationToken cancellationToken)
    {
        try
        {
            await _bannerService.ReorderBannersAsync(bannerIds, cancellationToken);
            return Ok(new { message = "Banners reordered successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Toggle banner status (Admin only)
    /// </summary>
    [HttpPost("banners/{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ToggleBannerStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bannerService.ToggleBannerStatusAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound("Banner not found");
        }

        return Ok(new { message = "Banner status toggled successfully" });
    }

    /// <summary>
    /// Get banner statistics (Admin only)
    /// </summary>
    [HttpGet("banners/statistics")]
    [ProducesResponseType(typeof(BannerStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BannerStatisticsDto>> GetBannerStatistics(CancellationToken cancellationToken)
    {
        var statistics = await _bannerService.GetBannerStatisticsAsync(cancellationToken);
        return Ok(statistics);
    }

    /// <summary>
    /// Test image serving (Public access for debugging)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("test-images")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult TestImageServing()
    {
        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsPath = Path.Combine(webRootPath, "uploads", "banners");
        
        var result = new
        {
            WebRootPath = webRootPath,
            UploadsPath = uploadsPath,
            DirectoryExists = Directory.Exists(uploadsPath),
            Files = Directory.Exists(uploadsPath) ? Directory.GetFiles(uploadsPath).Select(f => Path.GetFileName(f)).ToArray() : new string[0],
            BaseUrl = $"{Request.Scheme}://{Request.Host}"
        };
        
        return Ok(result);
    }

    /// <summary>
    /// Test email sending (Admin only)
    /// </summary>
    [HttpPost("test-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> TestEmail([FromBody] TestEmailDto testEmailDto, CancellationToken cancellationToken)
    {
        try
        {
            if (testEmailDto == null || string.IsNullOrWhiteSpace(testEmailDto.Email))
            {
                return BadRequest(new { error = "Email required", message = "Please provide a valid email address to test." });
            }

            var subject = testEmailDto.Subject ?? "SmartTeam - Test Email";
            var body = testEmailDto.Body ?? $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>SmartTeam - Test Email</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📧 SmartTeam</h1>
            <h2>Test Email</h2>
        </div>
        <div class='content'>
            <p>Salam,</p>
            <p>Bu bir test e-poçtudur. E-poçt konfiqurasiyanız düzgün işləyir!</p>
            <p>Test zamanı: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>
        <div class='footer'>
            <p>Bu e-poçt SmartTeam sistemi tərəfindən avtomatik göndərilmişdir.</p>
            <p>© 2024 SmartTeam. Bütün hüquqlar qorunur.</p>
        </div>
    </div>
</body>
</html>";

            var emailSent = await _emailService.SendEmailAsync(
                testEmailDto.Email,
                subject,
                body,
                true,
                cancellationToken);

            if (emailSent)
            {
                return Ok(new
                {
                    success = true,
                    message = "Test email sent successfully!",
                    recipient = testEmailDto.Email,
                    sentAt = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Failed to send email",
                    message = "Email could not be sent. Please check your email settings and SMTP configuration."
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "Email test failed",
                message = ex.Message
            });
        }
    }

    public class TestEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }



    /// <summary>
    /// Get active banners (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("banners/active")]
    [ProducesResponseType(typeof(IEnumerable<BannerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BannerDto>>> GetActiveBanners(CancellationToken cancellationToken)
    {
        var banners = await _bannerService.GetActiveBannersAsync(cancellationToken);
        return Ok(banners);
    }



    #endregion

    #region File Management

    /// <summary>
    /// Get all downloadable files (Admin only)
    /// </summary>
    [HttpGet("files")]
    [ProducesResponseType(typeof(IEnumerable<DownloadableFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DownloadableFileDto>>> GetAllFiles(CancellationToken cancellationToken)
    {
        var files = await _downloadableFileService.GetAllFilesAsync(cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Search and filter files with pagination (Admin only)
    /// </summary>
    [HttpGet("files/search")]
    [ProducesResponseType(typeof(PagedDownloadableFileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedDownloadableFileResultDto>> SearchFiles(
        [FromQuery] string? searchTerm,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] string? contentType,
        [FromQuery] long? minFileSize,
        [FromQuery] long? maxFileSize,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchDto = new DownloadableFileSearchDto
            {
                SearchTerm = searchTerm,
                Category = category,
                IsActive = isActive,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                ContentType = contentType,
                MinFileSize = minFileSize,
                MaxFileSize = maxFileSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _downloadableFileService.SearchFilesAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get file by ID (Admin only)
    /// </summary>
    [HttpGet("files/{id:guid}")]
    [ProducesResponseType(typeof(DownloadableFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DownloadableFileDto>> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var file = await _downloadableFileService.GetFileByIdAsync(id, cancellationToken);
        if (file == null)
        {
            return NotFound();
        }

        return Ok(file);
    }

    /// <summary>
    /// Upload a new file (Admin only)
    /// </summary>
    [HttpPost("files/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DownloadableFileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DownloadableFileDto>> UploadFile(
        [FromForm] CreateDownloadableFileDto createDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var file = await _downloadableFileService.UploadFileAsync(createDto, userId, cancellationToken);
            return CreatedAtAction(nameof(GetFile), new { id = file.Id }, file);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Update file information (Admin only)
    /// </summary>
    [HttpPut("files/{id:guid}")]
    [ProducesResponseType(typeof(DownloadableFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DownloadableFileDto>> UpdateFile(Guid id, [FromBody] UpdateDownloadableFileDto updateDto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var file = await _downloadableFileService.UpdateFileAsync(id, updateDto, userId, cancellationToken);
            if (file == null)
            {
                return NotFound();
            }
            return Ok(file);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Delete a file (Admin only)
    /// </summary>
    [HttpDelete("files/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteFile(Guid id, CancellationToken cancellationToken)
    {
        var result = await _downloadableFileService.DeleteFileAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound("File not found");
        }

        return Ok(new { message = "File deleted successfully" });
    }

    /// <summary>
    /// Toggle file status (Admin only)
    /// </summary>
    [HttpPost("files/{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ToggleFileStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _downloadableFileService.ToggleFileStatusAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound("File not found");
        }

        return Ok(new { message = "File status toggled successfully" });
    }

    /// <summary>
    /// Get file statistics (Admin only)
    /// </summary>
    [HttpGet("files/statistics")]
    [ProducesResponseType(typeof(DownloadableFileStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DownloadableFileStatisticsDto>> GetFileStatistics(CancellationToken cancellationToken)
    {
        var statistics = await _downloadableFileService.GetStatisticsAsync(cancellationToken);
        return Ok(statistics);
    }

    /// <summary>
    /// Get all file categories (Admin only)
    /// </summary>
    [HttpGet("files/categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<string>>> GetFileCategories(CancellationToken cancellationToken)
    {
        var categories = await _downloadableFileService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    #endregion

    #region Product PDF Management

    /// <summary>
    /// Get all product PDFs (Admin only)
    /// </summary>
    [HttpGet("product-pdfs")]
    [ProducesResponseType(typeof(IEnumerable<ProductPdfDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ProductPdfDto>>> GetAllProductPdfs(CancellationToken cancellationToken)
    {
        var pdfs = await _productPdfService.GetAllPdfsAsync(cancellationToken);
        return Ok(pdfs);
    }

    /// <summary>
    /// Search and filter product PDFs with pagination (Admin only)
    /// </summary>
    [HttpGet("product-pdfs/search")]
    [ProducesResponseType(typeof(PagedProductPdfResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedProductPdfResultDto>> SearchProductPdfs(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? productId,
        [FromQuery] bool? isActive,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] long? minFileSize,
        [FromQuery] long? maxFileSize,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchDto = new ProductPdfSearchDto
            {
                SearchTerm = searchTerm,
                ProductId = productId,
                IsActive = isActive,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                MinFileSize = minFileSize,
                MaxFileSize = maxFileSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _productPdfService.SearchPdfsAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get product PDF by ID (Admin only)
    /// </summary>
    [HttpGet("product-pdfs/{id:guid}")]
    [ProducesResponseType(typeof(ProductPdfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductPdfDto>> GetProductPdf(Guid id, CancellationToken cancellationToken)
    {
        var pdf = await _productPdfService.GetPdfByIdAsync(id, cancellationToken);
        if (pdf == null)
        {
            return NotFound();
        }

        return Ok(pdf);
    }

    /// <summary>
    /// Get product PDF by product ID (Admin only)
    /// </summary>
    [HttpGet("products/{productId:guid}/pdf")]
    [ProducesResponseType(typeof(ProductPdfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductPdfDto>> GetProductPdfByProductId(Guid productId, CancellationToken cancellationToken)
    {
        var pdf = await _productPdfService.GetPdfByProductIdAsync(productId, cancellationToken);
        if (pdf == null)
        {
            return NotFound();
        }

        return Ok(pdf);
    }

    /// <summary>
    /// Upload PDF for a product (Admin only)
    /// </summary>
    [HttpPost("products/{productId:guid}/pdf")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductPdfDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductPdfDto>> UploadProductPdf(
        Guid productId,
        [FromForm] CreateProductPdfDto createDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Override the productId from the route
            createDto.ProductId = productId;
            
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var pdf = await _productPdfService.UploadPdfAsync(createDto, userId, cancellationToken);
            return CreatedAtAction(nameof(GetProductPdf), new { id = pdf.Id }, pdf);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Update product PDF information (Admin only)
    /// </summary>
    [HttpPut("product-pdfs/{id:guid}")]
    [ProducesResponseType(typeof(ProductPdfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductPdfDto>> UpdateProductPdf(Guid id, [FromBody] UpdateProductPdfDto updateDto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var pdf = await _productPdfService.UpdatePdfAsync(id, updateDto, userId, cancellationToken);
            if (pdf == null)
            {
                return NotFound();
            }
            return Ok(pdf);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Delete a product PDF (Admin only)
    /// </summary>
    [HttpDelete("product-pdfs/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteProductPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productPdfService.DeletePdfAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound("PDF not found");
        }

        return Ok(new { message = "Product PDF deleted successfully" });
    }

    /// <summary>
    /// Toggle product PDF status (Admin only)
    /// </summary>
    [HttpPost("product-pdfs/{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ToggleProductPdfStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productPdfService.TogglePdfStatusAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound("PDF not found");
        }

        return Ok(new { message = "Product PDF status toggled successfully" });
    }

    /// <summary>
    /// Check if product has PDF (Admin only)
    /// </summary>
    [HttpGet("products/{productId:guid}/has-pdf")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> CheckProductHasPdf(Guid productId, CancellationToken cancellationToken)
    {
        var hasPdf = await _productPdfService.HasPdfAsync(productId, cancellationToken);
        return Ok(new { productId, hasPdf });
    }

    #endregion

    #region Database Management

    /// <summary>
    /// Clean all data from database (Development/Testing only)
    /// WARNING: This will delete ALL data including admin users
    /// </summary>
    [HttpPost("clean-database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CleanDatabase(CancellationToken cancellationToken)
    {
        try
        {
            // This will be implemented in the service layer
            await _productService.CleanAllDataAsync(cancellationToken);
            
            return Ok(new { 
                message = "Database cleaned successfully. Admin user will be recreated on next startup.",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to clean database", message = ex.Message });
        }
    }

    #endregion

    #region Category Management

    /// <summary>
    /// Add Azerbaijani categories to database (Admin only)
    /// </summary>
    [HttpPost("add-azerbaijani-categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddAzerbaijaniCategories(CancellationToken cancellationToken)
    {
        try
        {
            await _productService.AddAzerbaijaniCategoriesAsync(cancellationToken);
            
            return Ok(new { 
                message = "Azerbaijani categories added successfully!",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to add categories", message = ex.Message });
        }
    }

    #endregion

    #region Brand Management

    /// <summary>
    /// Get all brands (Admin only)
    /// </summary>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BrandDto>>> GetBrands(CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _brandService.GetAllBrandsAsync(cancellationToken);
            return Ok(brands);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Get brand by ID (Admin only)
    /// </summary>
    [HttpGet("brands/{id:guid}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDto>> GetBrand(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandService.GetBrandByIdAsync(id, cancellationToken);
            if (brand == null)
                return NotFound();

            return Ok(brand);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Create a new brand (Admin only)
    /// </summary>
    [HttpPost("brands")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BrandDto>> CreateBrand([FromBody] CreateBrandDto createBrandDto, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(createBrandDto.Name))
            {
                return BadRequest(new { error = "Brand name is required." });
            }

            // Use raw SQL approach that works
            var brandId = Guid.NewGuid();
            var brandName = createBrandDto.Name.Trim();
            var brandSlug = createBrandDto.Name.ToLower().Replace(" ", "-");
            var sortOrder = createBrandDto.SortOrder;
            var createdAt = DateTime.UtcNow;

            // Use raw SQL to insert the brand
            var sql = @"
                INSERT INTO Brand (Id, Name, Slug, IsActive, SortOrder, CreatedAt, UpdatedAt) 
                VALUES (@Id, @Name, @Slug, @IsActive, @SortOrder, @CreatedAt, @UpdatedAt)";

            var parameters = new[]
            {
                new Microsoft.Data.SqlClient.SqlParameter("@Id", brandId),
                new Microsoft.Data.SqlClient.SqlParameter("@Name", brandName),
                new Microsoft.Data.SqlClient.SqlParameter("@Slug", brandSlug),
                new Microsoft.Data.SqlClient.SqlParameter("@IsActive", true),
                new Microsoft.Data.SqlClient.SqlParameter("@SortOrder", sortOrder),
                new Microsoft.Data.SqlClient.SqlParameter("@CreatedAt", createdAt),
                new Microsoft.Data.SqlClient.SqlParameter("@UpdatedAt", DBNull.Value)
            };

            // Execute raw SQL
            var context = HttpContext.RequestServices.GetRequiredService<SmartTeam.Infrastructure.Data.SmartTeamDbContext>();
            await context.Database.ExecuteSqlRawAsync(sql, parameters);

            // Create the response DTO
            var brandDto = new BrandDto
            {
                Id = brandId,
                Name = brandName,
                Slug = brandSlug,
                IsActive = true,
                SortOrder = sortOrder,
                CreatedAt = createdAt
            };

            return CreatedAtAction(nameof(GetBrand), new { id = brandId }, brandDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                error = ex.Message, 
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Test brand creation with direct database access
    /// </summary>
    [HttpPost("brands-test")]
    public async Task<ActionResult> TestBrandCreation([FromBody] CreateBrandDto createBrandDto, CancellationToken cancellationToken)
    {
        try
        {
            // Create a simple brand object for testing
            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                Name = createBrandDto.Name,
                Slug = createBrandDto.Name.ToLower(),
                IsActive = true,
                SortOrder = createBrandDto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };

            return Ok(new { 
                message = "Brand object created successfully",
                brand = new {
                    brand.Id,
                    brand.Name,
                    brand.Slug,
                    brand.IsActive,
                    brand.SortOrder,
                    brand.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test brand creation with raw SQL
    /// </summary>
    [HttpPost("brands-raw-sql")]
    public async Task<ActionResult> TestBrandCreationRawSql([FromBody] CreateBrandDto createBrandDto, CancellationToken cancellationToken)
    {
        try
        {
            var brandId = Guid.NewGuid();
            var brandName = createBrandDto.Name.Trim();
            var brandSlug = createBrandDto.Name.ToLower().Replace(" ", "-");
            var sortOrder = createBrandDto.SortOrder;
            var createdAt = DateTime.UtcNow;

            // Use raw SQL to insert the brand
            var sql = @"
                INSERT INTO Brand (Id, Name, Slug, IsActive, SortOrder, CreatedAt, UpdatedAt) 
                VALUES (@Id, @Name, @Slug, @IsActive, @SortOrder, @CreatedAt, @UpdatedAt)";

            var parameters = new[]
            {
                new Microsoft.Data.SqlClient.SqlParameter("@Id", brandId),
                new Microsoft.Data.SqlClient.SqlParameter("@Name", brandName),
                new Microsoft.Data.SqlClient.SqlParameter("@Slug", brandSlug),
                new Microsoft.Data.SqlClient.SqlParameter("@IsActive", true),
                new Microsoft.Data.SqlClient.SqlParameter("@SortOrder", sortOrder),
                new Microsoft.Data.SqlClient.SqlParameter("@CreatedAt", createdAt),
                new Microsoft.Data.SqlClient.SqlParameter("@UpdatedAt", DBNull.Value)
            };

            // Execute raw SQL
            var context = HttpContext.RequestServices.GetRequiredService<SmartTeam.Infrastructure.Data.SmartTeamDbContext>();
            await context.Database.ExecuteSqlRawAsync(sql, parameters);

            return Ok(new { 
                message = "Brand created successfully with raw SQL",
                brand = new {
                    Id = brandId,
                    Name = brandName,
                    Slug = brandSlug,
                    IsActive = true,
                    SortOrder = sortOrder,
                    CreatedAt = createdAt
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                error = ex.Message, 
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Add predefined brands to database (Admin only)
    /// </summary>
    [HttpPost("add-brands")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddBrands(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _brandService.AddPredefinedBrandsAsync(cancellationToken);
            
            return Ok(new { 
                message = "Brands processed successfully!",
                addedCount = result.AddedCount,
                skippedCount = result.SkippedCount,
                totalRequested = result.TotalRequested,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to add brands", message = ex.Message });
        }
    }

    #endregion
}
