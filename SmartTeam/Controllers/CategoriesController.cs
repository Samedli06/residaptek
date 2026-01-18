using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve categories.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get root categories (categories without parent)
    /// </summary>
    [HttpGet("root")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetRootCategories(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetRootCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CategoryDto>> GetCategory(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid category ID.", message = "Category ID cannot be empty." });
            }

            var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
            if (category == null)
            {
                return NotFound(new { error = "Category not found.", message = $"No category found with ID: {id}" });
            }

            return Ok(category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid category parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve category.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get category by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryBySlugAsync(slug, cancellationToken);
        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    /// <summary>
    /// Get subcategories of a category
    /// </summary>
    [HttpGet("{id:guid}/subcategories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetSubCategories(Guid id, CancellationToken cancellationToken)
    {
        var subCategories = await _categoryService.GetSubCategoriesAsync(id, cancellationToken);
        return Ok(subCategories);
    }

    /// <summary>
    /// Create a new category (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createCategoryDto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.CreateCategoryAsync(createCategoryDto, cancellationToken);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Create a new category with image upload (Admin only)
    /// Accepts JSON data for category info and multipart form-data for image
    /// </summary>
    [HttpPost("with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryDto>> CreateCategoryWithImage(
        [FromForm] string? categoryData, // JSON string containing category information
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(categoryData))
            {
                return BadRequest("categoryData field is required and must contain valid JSON");
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("imageFile is required");
            }

            // Parse JSON category data
            CreateCategoryWithImageDto createCategoryDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                createCategoryDto = System.Text.Json.JsonSerializer.Deserialize<CreateCategoryWithImageDto>(categoryData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest($"Invalid JSON format for categoryData: {ex.Message}");
            }

            if (createCategoryDto == null)
            {
                return BadRequest("Failed to parse category data");
            }

            var category = await _categoryService.CreateCategoryWithImageAsync(createCategoryDto, imageFile, cancellationToken);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing category (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto updateCategoryDto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.UpdateCategoryAsync(id, updateCategoryDto, cancellationToken);
            return Ok(category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing category with image (Admin only)
    /// </summary>
    /// <remarks>
    /// Accepts multipart/form-data with 'categoryData' as JSON string and optional 'imageFile'.
    /// Use this endpoint when you need to update both category data and image simultaneously.
    /// </remarks>
    [HttpPut("{id:guid}/with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<ActionResult<CategoryDto>> UpdateCategoryWithImage(
        Guid id,
        [FromForm] string categoryData,
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoryData))
            {
                return BadRequest("categoryData field is required and must contain valid JSON");
            }

            UpdateCategoryWithImageDto updateCategoryDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                updateCategoryDto = System.Text.Json.JsonSerializer.Deserialize<UpdateCategoryWithImageDto>(categoryData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest($"Invalid JSON format for categoryData: {ex.Message}");
            }

            if (updateCategoryDto == null)
            {
                return BadRequest("Failed to parse category data");
            }

            // Update category with or without image
            var updatedCategory = await _categoryService.UpdateCategoryWithImageAsync(id, updateCategoryDto, imageFile, cancellationToken);
            return Ok(updatedCategory);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a category (soft delete) (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Upload a single image for a category (Admin only)
    /// </summary>
    [HttpPost("{id}/upload-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryDto>> UploadCategoryImage(
        Guid id, 
        IFormFile imageFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryService.UploadCategoryImageAsync(id, imageFile, cancellationToken);
            return Ok(category);
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
    /// Delete a category image (Admin only)
    /// </summary>
    [HttpDelete("{id}/delete-image")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteCategoryImage(
        Guid id, 
        [FromQuery] string imageUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _categoryService.DeleteCategoryImageAsync(id, imageUrl, cancellationToken);
            if (deleted)
            {
                return Ok(new { message = "Image deleted successfully" });
            }
            return NotFound("Image not found");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
