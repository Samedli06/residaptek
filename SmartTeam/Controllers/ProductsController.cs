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
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IFilterService _filterService;

    public ProductsController(IProductService productService, IFilterService filterService)
    {
        _productService = productService;
        _filterService = filterService;
    }

    /// <summary>
    /// Get all products with role-based pricing and favorite status
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            
            IEnumerable<ProductListDto> products;
            
            if (categoryId.HasValue)
            {
                if (categoryId.Value == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid category ID format.", message = "Category ID cannot be empty." });
                }
                
                products = await _productService.GetProductsByCategoryAsync(categoryId.Value, userRole, userId, cancellationToken);
            }
            else
            {
                products = await _productService.GetAllProductsAsync(userRole, userId, cancellationToken);
            }
            
            return Ok(products);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get products by category with role-based pricing
    /// </summary>
    [HttpGet("category/{categoryId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProductsByCategory(Guid categoryId, CancellationToken cancellationToken)
    {
        try
        {
            if (categoryId == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid category ID.", message = "Category ID cannot be empty." });
            }

            var userRole = GetCurrentUserRole();
            var products = await _productService.GetProductsByCategoryAsync(categoryId, userRole, cancellationToken);
            
            if (!products.Any())
            {
                return NotFound(new { error = "No products found.", message = $"No products found in category with ID: {categoryId}" });
            }

            return Ok(products);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid category parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products by category.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get products by category slug with role-based pricing and favorite status
    /// </summary>
    [HttpGet("category/slug/{categorySlug}")]
    [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProductsByCategorySlug(string categorySlug, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categorySlug))
            {
                return BadRequest(new { error = "Invalid category slug.", message = "Category slug cannot be empty or whitespace." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var products = await _productService.GetProductsByCategorySlugAsync(categorySlug, userRole, userId, cancellationToken);
            
            if (!products.Any())
            {
                return NotFound(new { error = "No products found.", message = $"No products found in category with slug: '{categorySlug}'" });
            }

            return Ok(products);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid category parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products by category.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get hot deals with role-based pricing
    /// </summary>
    [HttpGet("hot-deals")]
    [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetHotDeals([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var userRole = GetCurrentUserRole();
        var products = await _productService.GetHotDealsAsync(userRole, limit, cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Get product by ID with role-based pricing and favorite status
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid product ID.", message = "Product ID cannot be empty." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var product = await _productService.GetProductByIdAsync(id, userRole, userId, cancellationToken);
            
            if (product == null)
            {
                return NotFound(new { error = "Product not found.", message = $"No product found with ID: {id}" });
            }

            return Ok(product);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving the product.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get product by slug with role-based pricing
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> GetProductBySlug(string slug, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest(new { error = "Invalid product slug.", message = "Product slug cannot be empty or whitespace." });
            }

            var userRole = GetCurrentUserRole();
            var product = await _productService.GetProductBySlugAsync(slug, userRole, cancellationToken);
            
            if (product == null)
            {
                return NotFound(new { error = "Product not found.", message = $"No product found with slug: '{slug}'" });
            }

            return Ok(product);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving the product.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get product with pricing for current user role
    /// </summary>
    [HttpGet("{id:guid}/with-pricing")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductWithPricing(Guid id, CancellationToken cancellationToken)
    {
        var userRole = GetCurrentUserRole();
        var product = await _productService.GetProductByIdAsync(id, userRole, cancellationToken);
        
        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    // GetProductWithAllPrices removed as it is no longer needed with unified pricing.

    /// <summary>
    /// Search products with role-based pricing
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> SearchProducts([FromQuery] string q, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { error = "Invalid search term.", message = "Search term is required and cannot be empty." });
            }

            if (q.Length < 2)
            {
                return BadRequest(new { error = "Search term too short.", message = "Search term must be at least 2 characters long." });
            }

            if (q.Length > 100)
            {
                return BadRequest(new { error = "Search term too long.", message = "Search term cannot exceed 100 characters." });
            }

            var userRole = GetCurrentUserRole();
            var products = await _productService.SearchProductsAsync(q, userRole, cancellationToken);
            return Ok(products);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid search parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while searching products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Global search across categories, brands, and products
    /// </summary>
    [HttpGet("global-search")]
    [ProducesResponseType(typeof(GlobalSearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GlobalSearchResultDto>> GlobalSearch([FromQuery] string q, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { error = "Invalid search term.", message = "Search term is required and cannot be empty." });
            }

            if (q.Length < 2)
            {
                return BadRequest(new { error = "Search term too short.", message = "Search term must be at least 2 characters long." });
            }

            if (q.Length > 100)
            {
                return BadRequest(new { error = "Search term too long.", message = "Search term cannot exceed 100 characters." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var results = await _productService.GlobalSearchAsync(q, userRole, userId, cancellationToken);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid search parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while performing global search.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get recommended products based on user preferences and behavior
    /// </summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(RecommendedProductsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecommendedProductsDto>> GetRecommendedProducts(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? categoryId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var userRole = GetCurrentUserRole();
        var userId = GetCurrentUserId();
        
        var request = new RecommendationRequestDto
        {
            ProductId = productId,
            CategoryId = categoryId,
            Limit = limit
        };
        
        var recommendations = await _productService.GetRecommendedProductsAsync(request, userRole, userId, cancellationToken);
        return Ok(recommendations);
    }

    /// <summary>
    /// Get all available filters for products
    /// </summary>
    [HttpGet("filters")]
    [ProducesResponseType(typeof(IEnumerable<FilterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FilterDto>>> GetAvailableFilters(CancellationToken cancellationToken)
    {
        var filters = await _filterService.GetPublicFiltersAsync(cancellationToken);
        return Ok(filters);
    }

    /// <summary>
    /// Get available filters for a specific category
    /// </summary>
    [HttpGet("category/{categoryId:guid}/filters")]
    [ProducesResponseType(typeof(IEnumerable<FilterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FilterDto>>> GetFiltersForCategory(Guid categoryId, CancellationToken cancellationToken)
    {
        var filters = await _filterService.GetPublicFiltersForCategoryAsync(categoryId, cancellationToken);
        return Ok(filters);
    }

    /// <summary>
    /// Get products filtered by custom filter criteria
    /// </summary>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(FilteredProductsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FilteredProductsResultDto>> GetFilteredProducts([FromBody] ProductFilterCriteriaDto criteria, CancellationToken cancellationToken)
    {
        try
        {
            if (criteria == null)
            {
                return BadRequest(new { error = "Invalid filter criteria.", message = "Filter criteria cannot be null." });
            }

            if (criteria.Page <= 0)
            {
                return BadRequest(new { error = "Invalid page number.", message = "Page number must be greater than 0." });
            }

            if (criteria.PageSize <= 0 || criteria.PageSize > 100)
            {
                return BadRequest(new { error = "Invalid page size.", message = "Page size must be between 1 and 100." });
            }

            if (criteria.MinPrice.HasValue && criteria.MinPrice.Value < 0)
            {
                return BadRequest(new { error = "Invalid minimum price.", message = "Minimum price cannot be negative." });
            }

            if (criteria.MaxPrice.HasValue && criteria.MaxPrice.Value < 0)
            {
                return BadRequest(new { error = "Invalid maximum price.", message = "Maximum price cannot be negative." });
            }

            if (criteria.MinPrice.HasValue && criteria.MaxPrice.HasValue && criteria.MinPrice.Value > criteria.MaxPrice.Value)
            {
                return BadRequest(new { error = "Invalid price range.", message = "Minimum price cannot be greater than maximum price." });
            }

            var userRole = GetCurrentUserRole();
            var result = await _productService.GetFilteredProductsAsync(criteria, userRole, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid filter parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while filtering products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Diagnostic endpoint to analyze category structure and products (Admin only)
    /// </summary>
    [HttpGet("diagnose-categories")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> DiagnoseCategoryStructure([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        var diagnosis = await _productService.DiagnoseCategoryStructureAsync(categoryId, cancellationToken);
        return Ok(diagnosis);
    }

    /// <summary>
    /// Test parent category filtering logic (Admin only)
    /// </summary>
    [HttpGet("test-parent-category/{parentCategoryId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> TestParentCategoryFiltering(Guid parentCategoryId, CancellationToken cancellationToken)
    {
        var result = await _productService.TestParentCategoryFilteringAsync(parentCategoryId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get product specifications and attributes
    /// </summary>
    [HttpGet("{id:guid}/specifications")]
    [ProducesResponseType(typeof(ProductSpecificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductSpecificationDto>> GetProductSpecifications(Guid id, CancellationToken cancellationToken)
    {
        var specifications = await _productService.GetProductSpecificationsAsync(id, cancellationToken);
        
        if (specifications == null)
        {
            return NotFound();
        }

        return Ok(specifications);
    }

    /// <summary>
    /// Create product specifications (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/specifications")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductSpecificationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductSpecificationDto>> CreateProductSpecifications(
        Guid id,
        [FromBody] CreateProductSpecificationDto createDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure the product ID matches
            createDto.ProductId = id;
            
            var specifications = await _productService.CreateProductSpecificationsAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetProductSpecifications), new { id }, specifications);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update product specifications (Admin only)
    /// </summary>
    [HttpPut("{id:guid}/specifications")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductSpecificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductSpecificationDto>> UpdateProductSpecifications(
        Guid id,
        [FromBody] UpdateProductSpecificationDto updateDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var specifications = await _productService.UpdateProductSpecificationsAsync(id, updateDto, cancellationToken);
            return Ok(specifications);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete product specifications (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}/specifications")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProductSpecifications(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _productService.DeleteProductSpecificationsAsync(id, cancellationToken);
        
        if (!deleted)
        {
            return NotFound("No specifications found for this product.");
        }

        return NoContent();
    }

    /// <summary>
    /// Create a new product (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.CreateProductAsync(createProductDto, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
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
    /// Create a new product with image upload (Admin only)
    /// Accepts JSON data for product info and multipart form-data for image
    /// </summary>
    [HttpPost("with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> CreateProductWithImage(
        [FromForm] string? productData, // JSON string containing product information
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(productData))
            {
                return BadRequest("productData field is required and must contain valid JSON");
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("imageFile is required");
            }

            CreateProductWithImageDto createProductDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                createProductDto = System.Text.Json.JsonSerializer.Deserialize<CreateProductWithImageDto>(productData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest($"Invalid JSON format for productData: {ex.Message}");
            }

            if (createProductDto == null)
            {
                return BadRequest("Failed to parse product data");
            }

            var product = await _productService.CreateProductWithImageAsync(createProductDto, imageFile, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
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
    /// Update an existing product (Admin only)
    /// </summary>
    /// <remarks>
    /// Supports JSON request body with UpdateProductDto.
    /// Prices can be updated by including them in the Prices array.
    /// BrandId can be updated by including it in the request.
    /// For image updates, use the dedicated image upload endpoints.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductDto updateProductDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.UpdateProductAsync(id, updateProductDto, cancellationToken);
            return Ok(product);
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
    /// Update an existing product with image (Admin only)
    /// </summary>
    /// <remarks>
    /// Accepts multipart/form-data with 'productData' as JSON string and optional 'imageFile'.
    /// Use this endpoint when you need to update both product data and image simultaneously.
    /// </remarks>
    [HttpPut("{id:guid}/with-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<ActionResult<ProductDto>> UpdateProductWithImage(
        Guid id,
        [FromForm] string productData,
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(productData))
            {
                return BadRequest("productData field is required and must contain valid JSON");
            }

            UpdateProductDto updateProductDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                updateProductDto = System.Text.Json.JsonSerializer.Deserialize<UpdateProductDto>(productData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest($"Invalid JSON format for productData: {ex.Message}");
            }

            if (updateProductDto == null)
            {
                return BadRequest("Failed to parse product data");
            }

            // Update product with or without image
            var updatedProduct = await _productService.UpdateProductWithImageAsync(id, updateProductDto, imageFile, cancellationToken);
            return Ok(updatedProduct);
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
    /// Update an existing product with all files (image, detail images array, and PDF) (Admin only)
    /// </summary>
    /// <remarks>
    /// Accepts multipart/form-data with 'productData' as JSON string and optional 'imageFile', 'detailImageFiles' (array), and 'pdfFile'.
    /// Use this endpoint when you need to update product data along with SKU, images, and PDF files.
    /// Supports multiple detail images upload and management.
    /// </remarks>
    [HttpPut("{id:guid}/with-files")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<ActionResult<ProductDto>> UpdateProductWithFiles(
        Guid id,
        [FromForm] string productData,
        IFormFile? imageFile,
        IFormFile[]? detailImageFiles,
        IFormFile? pdfFile,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(productData))
            {
                return BadRequest("productData field is required and must contain valid JSON");
            }

            UpdateProductDto updateProductDto;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                updateProductDto = System.Text.Json.JsonSerializer.Deserialize<UpdateProductDto>(productData, options)!;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest($"Invalid JSON format for productData: {ex.Message}");
            }

            if (updateProductDto == null)
            {
                return BadRequest("Failed to parse product data");
            }

            // Get current user ID for PDF tracking
            var userId = GetCurrentUserId();

            // Update product with optional files
            var updatedProduct = await _productService.UpdateProductWithFilesAsync(
                id, 
                updateProductDto, 
                imageFile, 
                detailImageFiles, 
                pdfFile, 
                userId, 
                cancellationToken);
            return Ok(updatedProduct);
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
    /// Delete a product (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteProductAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound();
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

    /// <summary>
    /// Upload a single image for a product (Admin only)
    /// </summary>
    [HttpPost("{id}/upload-image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> UploadProductImage(
        Guid id, 
        IFormFile imageFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productService.UploadProductImageAsync(id, imageFile, cancellationToken);
            return Ok(product);
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
    /// Upload multiple images for a product (Admin only)
    /// </summary>
    [HttpPost("{id}/upload-images")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> UploadProductImages(
        Guid id, 
        IFormFileCollection imageFiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productService.UploadProductImagesAsync(id, imageFiles, cancellationToken);
            return Ok(product);
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
    /// Delete a product image (Admin only)
    /// </summary>
    [HttpDelete("{id}/delete-image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteProductImage(
        Guid id, 
        [FromQuery] string imageUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _productService.DeleteProductImageAsync(id, imageUrl, cancellationToken);
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

    /// <summary>
    /// Delete a product detail image by image ID (Admin only)
    /// </summary>
    [HttpDelete("images/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteProductDetailImage(
        Guid id, // Image ID
        CancellationToken cancellationToken = default)
    {
        try
        {
            System.Console.WriteLine($"[Controller] DeleteProductDetailImage called with ImageId: {id}");
            
            var deleted = await _productService.DeleteProductDetailImageByIdAsync(id, cancellationToken);
            
            if (deleted)
            {
                System.Console.WriteLine($"[Controller] Image deleted successfully: {id}");
                return Ok(new { message = "Detail image deleted successfully" });
            }
            
            System.Console.WriteLine($"[Controller] Image not found or deletion failed: {id}");
            return NotFound(new { message = "Detail image not found", imageId = id });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Controller] EXCEPTION in DeleteProductDetailImage: {ex.Message}");
            System.Console.WriteLine($"[Controller] StackTrace: {ex.StackTrace}");
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }

    /// <summary>
    /// Get products by brand
    /// </summary>
    [HttpGet("brand/{brandSlug}")]
    [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProductsByBrand(string brandSlug, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brandSlug))
            {
                return BadRequest(new { error = "Invalid brand slug.", message = "Brand slug cannot be empty or whitespace." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var products = await _productService.GetProductsByBrandAsync(brandSlug, userRole, userId, cancellationToken);
            
            if (!products.Any())
            {
                return NotFound(new { error = "No products found.", message = $"No products found for brand: {brandSlug}" });
            }

            return Ok(products);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    #region Pagination Endpoints

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PagedResultDto<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductListDto>>> GetProductsPaginated([FromQuery] ProductPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductsPaginatedAsync(request, userRole, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get products by category with pagination
    /// </summary>
    [HttpGet("category/{categoryId:guid}/paginated")]
    [ProducesResponseType(typeof(PagedResultDto<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductListDto>>> GetProductsByCategoryPaginated(Guid categoryId, [FromQuery] ProductPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (categoryId == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid category ID.", message = "Category ID cannot be empty." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductsByCategoryPaginatedAsync(categoryId, request, userRole, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get products by category slug with pagination
    /// </summary>
    [HttpGet("category/slug/{categorySlug}/paginated")]
    [ProducesResponseType(typeof(PagedResultDto<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductListDto>>> GetProductsByCategorySlugPaginated(string categorySlug, [FromQuery] ProductPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categorySlug))
            {
                return BadRequest(new { error = "Invalid category slug.", message = "Category slug cannot be empty or whitespace." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductsByCategorySlugPaginatedAsync(categorySlug, request, userRole, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get products by brand with pagination
    /// </summary>
    [HttpGet("brand/{brandSlug}/paginated")]
    [ProducesResponseType(typeof(PagedResultDto<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductListDto>>> GetProductsByBrandPaginated(string brandSlug, [FromQuery] ProductPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brandSlug))
            {
                return BadRequest(new { error = "Invalid brand slug.", message = "Brand slug cannot be empty or whitespace." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var result = await _productService.GetProductsByBrandPaginatedAsync(brandSlug, request, userRole, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get hot deals with pagination
    /// </summary>
    [HttpGet("hot-deals/paginated")]
    [ProducesResponseType(typeof(PagedResultDto<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductListDto>>> GetHotDealsPaginated([FromQuery] HotDealsPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var result = await _productService.GetHotDealsPaginatedAsync(request, userRole, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving hot deals.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Search products with pagination
    /// </summary>
    [HttpGet("search/paginated")]
    [ProducesResponseType(typeof(PagedResultDto<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductListDto>>> SearchProductsPaginated([FromQuery] SearchPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return BadRequest(new { error = "Invalid search term.", message = "Search term is required and cannot be empty." });
            }

            if (request.SearchTerm.Length < 2)
            {
                return BadRequest(new { error = "Search term too short.", message = "Search term must be at least 2 characters long." });
            }

            if (request.SearchTerm.Length > 100)
            {
                return BadRequest(new { error = "Search term too long.", message = "Search term cannot exceed 100 characters." });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var result = await _productService.SearchProductsPaginatedAsync(request, userRole, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid search parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while searching products.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get recommended products with pagination
    /// </summary>
    [HttpGet("recommendations/paginated")]
    [ProducesResponseType(typeof(PagedResultDto<ProductListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<ProductListDto>>> GetRecommendedProductsPaginated([FromQuery] RecommendedProductsPaginationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();
            var result = await _productService.GetRecommendedProductsPaginatedAsync(request, userRole, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid request parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error occurred while retrieving recommendations.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    #endregion
}
