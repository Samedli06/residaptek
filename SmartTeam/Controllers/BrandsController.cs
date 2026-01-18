using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    /// <summary>
    /// Get all brands
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BrandDto>>> GetBrands(CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _brandService.GetAllBrandsAsync(cancellationToken);
            return Ok(brands);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve brands.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get brand by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> GetBrandById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandService.GetBrandByIdAsync(id, cancellationToken);
            if (brand == null)
            {
                return NotFound(new { error = "Brand not found.", message = "The requested brand could not be found." });
            }
            return Ok(brand);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve brand.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get brand by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> GetBrandBySlug(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _brandService.GetBrandBySlugAsync(slug, cancellationToken);
            if (brand == null)
            {
                return NotFound(new { error = "Brand not found.", message = "The requested brand could not be found." });
            }
            return Ok(brand);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve brand.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Create a new brand (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> CreateBrand([FromBody] CreateBrandDto createBrandDto, CancellationToken cancellationToken)
    {
        try
        {
            if (createBrandDto == null)
            {
                return BadRequest(new { error = "Invalid brand data.", message = "Brand data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(createBrandDto.Name))
            {
                return BadRequest(new { error = "Brand name required.", message = "Brand name is required." });
            }

            var brand = await _brandService.CreateBrandAsync(createBrandDto, cancellationToken);
            return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, brand);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid brand data.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create brand.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Create a new brand with image upload (Admin only)
    /// </summary>
    [HttpPost("with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> CreateBrandWithImage(
        string name,
        int sortOrder,
        IFormFile imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { error = "Brand name required.", message = "Brand name is required." });
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest(new { error = "Image file required.", message = "Brand logo image is required." });
            }

            var createBrandDto = new CreateBrandWithImageDto
            {
                Name = name,
                SortOrder = sortOrder
            };

            var brand = await _brandService.CreateBrandWithImageAsync(createBrandDto, imageFile, cancellationToken);
            return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, brand);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid brand data.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create brand.", message = ex.Message, details = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Update an existing brand (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BrandDto>> UpdateBrand(Guid id, [FromBody] UpdateBrandDto updateBrandDto, CancellationToken cancellationToken)
    {
        try
        {
            if (updateBrandDto == null)
            {
                return BadRequest(new { error = "Invalid brand data.", message = "Brand data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(updateBrandDto.Name))
            {
                return BadRequest(new { error = "Brand name required.", message = "Brand name is required." });
            }

            var brand = await _brandService.UpdateBrandAsync(id, updateBrandDto, cancellationToken);
            return Ok(brand);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = "Brand not found.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update brand.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Update an existing brand with image (Admin only)
    /// </summary>
    /// <remarks>
    /// Accepts multipart/form-data with 'brandData' as JSON string and optional 'imageFile'.
    /// Use this endpoint when you need to update both brand data and image simultaneously.
    /// </remarks>
    [HttpPut("{id:guid}/with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<ActionResult<BrandDto>> UpdateBrandWithImage(
        Guid id,
        [FromForm] string brandData,
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brandData))
            {
                return BadRequest(new { error = "Brand data required.", message = "brandData field is required and must contain valid JSON" });
            }

            UpdateBrandWithImageDto updateBrandDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                updateBrandDto = System.Text.Json.JsonSerializer.Deserialize<UpdateBrandWithImageDto>(brandData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest(new { error = "Invalid JSON format.", message = $"Invalid JSON format for brandData: {ex.Message}" });
            }

            if (updateBrandDto == null)
            {
                return BadRequest(new { error = "Failed to parse brand data.", message = "Failed to parse brand data" });
            }

            if (string.IsNullOrWhiteSpace(updateBrandDto.Name))
            {
                return BadRequest(new { error = "Brand name required.", message = "Brand name is required." });
            }

            // Update brand with or without image
            var updatedBrand = await _brandService.UpdateBrandWithImageAsync(id, updateBrandDto, imageFile, cancellationToken);
            return Ok(updatedBrand);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = "Brand not found.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update brand.", message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a brand (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _brandService.DeleteBrandAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(new { error = "Brand not found.", message = "The requested brand could not be found." });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to delete brand.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Add predefined brands (Admin only)
    /// </summary>
    [HttpPost("add-predefined")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AddBrandsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AddBrandsResultDto>> AddPredefinedBrands(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _brandService.AddPredefinedBrandsAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to add predefined brands.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get all brands with pagination
    /// </summary>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PagedResultDto<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<BrandDto>>> GetBrandsPaginated([FromQuery] BrandPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _brandService.GetBrandsPaginatedAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve brands.", message = "Please try again later or contact support if the issue persists." });
        }
    }
}
