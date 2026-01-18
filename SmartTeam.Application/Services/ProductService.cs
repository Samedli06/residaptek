using AutoMapper;
using Microsoft.AspNetCore.Http;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using System.Net;

namespace SmartTeam.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileUploadService _fileUploadService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, IFileUploadService fileUploadService, ICategoryService categoryService, IBrandService brandService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileUploadService = fileUploadService;
        _categoryService = categoryService;
        _brandService = brandService;
    }

    public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync(UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        var activeProducts = products.Where(p => p.IsActive);

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(activeProducts);
        return productDtos;
    }

    public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync(UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        var activeProducts = products.Where(p => p.IsActive);

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(activeProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return productDtos;
    }

    public async Task<IEnumerable<ProductListDto>> GetProductsByCategoryAsync(Guid categoryId, UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        var categoryIds = await GetCategoryIdsIncludingSubcategories(categoryId, cancellationToken);
        var filteredProducts = products.Where(p => categoryIds.Contains(p.CategoryId) && p.IsActive);

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(filteredProducts);
        
        return productDtos;
    }

    public async Task<IEnumerable<ProductListDto>> GetProductsByCategoryAsync(Guid categoryId, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        var categoryIds = await GetCategoryIdsIncludingSubcategories(categoryId, cancellationToken);
        var filteredProducts = products.Where(p => categoryIds.Contains(p.CategoryId) && p.IsActive);

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(filteredProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return productDtos;
    }

    public async Task<IEnumerable<ProductListDto>> GetProductsByCategorySlugAsync(string categorySlug, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            throw new ArgumentException("Category slug cannot be null or empty.", nameof(categorySlug));
        }

        // Find category by slug
        var category = await _unitOfWork.Repository<Category>()
            .FirstOrDefaultAsync(c => c.Slug == categorySlug && c.IsActive, cancellationToken);
        
        if (category == null)
        {
            throw new ArgumentException($"Category with slug '{categorySlug}' not found.");
        }

        // Use existing method to get products by category ID
        return await GetProductsByCategoryAsync(category.Id, userRole, userId, cancellationToken);
    }

    public async Task<IEnumerable<ProductListDto>> GetHotDealsAsync(UserRole? userRole = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        var hotDeals = products.Where(p => p.IsHotDeal && p.IsActive);

        // Apply limit if specified
        if (limit.HasValue && limit.Value > 0)
        {
            hotDeals = hotDeals.Take(limit.Value);
        }

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(hotDeals);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return productDtos;
    }

    public async Task<IEnumerable<ProductListDto>> GetProductsByBrandAsync(string brandSlug, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        // First, get the brand by slug
        var brand = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Slug == brandSlug && b.IsActive, cancellationToken);
        
        if (brand == null)
            return new List<ProductListDto>();

        // Get products for this brand
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Brand);
        var brandProducts = products.Where(p => p.BrandId == brand.Id && p.IsActive);

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(brandProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return productDtos;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdWithIncludesAsync(id, p => p.Category, p => p.Images);
        
        if (product == null || !product.IsActive)
            return null;

        var productDto = _mapper.Map<ProductDto>(product);
        
        var images = await _unitOfWork.Repository<ProductImage>()
            .FindAsync(pi => pi.ProductId == id, cancellationToken);
        
        if (!images.Any() && !string.IsNullOrEmpty(product.ImageUrl))
        {
            var mainImageAsProductImage = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = id,
                ImageUrl = product.ImageUrl,
                IsPrimary = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ProductImage>().AddAsync(mainImageAsProductImage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            images = new[] { mainImageAsProductImage };
        }
        
        productDto.Images = _mapper.Map<List<ProductImageDto>>(images.OrderBy(i => i.SortOrder));
        
        await PopulateCategoryBreadcrumbsForSingleProduct(productDto, cancellationToken);
        return productDto;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdWithIncludesAsync(id, p => p.Category, p => p.Images, p => p.Brand);
        
        if (product == null || !product.IsActive)
            return null;

        var productDto = _mapper.Map<ProductDto>(product);
        
        var images = await _unitOfWork.Repository<ProductImage>()
            .FindAsync(pi => pi.ProductId == id, cancellationToken);
        
        if (!images.Any() && !string.IsNullOrEmpty(product.ImageUrl))
        {
            var mainImageAsProductImage = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = id,
                ImageUrl = product.ImageUrl,
                IsPrimary = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ProductImage>().AddAsync(mainImageAsProductImage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            images = new[] { mainImageAsProductImage };
        }
        
        productDto.Images = _mapper.Map<List<ProductImageDto>>(images.OrderBy(i => i.SortOrder));
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProduct(productDto, userId.Value, cancellationToken);
        }
        
        await PopulateCategoryBreadcrumbsForSingleProduct(productDto, cancellationToken);
        return productDto;
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug, UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>()
            .FirstOrDefaultWithIncludesAsync(p => p.Slug == slug && p.IsActive, p => p.Category, p => p.Images, p => p.Brand);

        if (product == null)
            return null;

        var productDto = _mapper.Map<ProductDto>(product);
        
        var images = await _unitOfWork.Repository<ProductImage>()
            .FindAsync(pi => pi.ProductId == product.Id, cancellationToken);
        
        if (!images.Any() && !string.IsNullOrEmpty(product.ImageUrl))
        {
            var mainImageAsProductImage = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImageUrl = product.ImageUrl,
                IsPrimary = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ProductImage>().AddAsync(mainImageAsProductImage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            images = new[] { mainImageAsProductImage };
        }
        
        productDto.Images = _mapper.Map<List<ProductImageDto>>(images.OrderBy(i => i.SortOrder));
        
        await PopulateCategoryBreadcrumbsForSingleProduct(productDto, cancellationToken);
        return productDto;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _unitOfWork.Repository<Category>()
            .AnyAsync(c => c.Id == createProductDto.CategoryId && c.IsActive, cancellationToken);
        
        if (!categoryExists)
        {
            throw new ArgumentException("Category not found or inactive.");
        }

        // Check if product with same name already exists
        var existingProductName = await _unitOfWork.Repository<Product>()
            .AnyAsync(p => p.Name == createProductDto.Name, cancellationToken);
        
        if (existingProductName)
        {
            throw new InvalidOperationException("A product with this name already exists.");
        }

        var existingSku = await _unitOfWork.Repository<Product>()
            .AnyAsync(p => p.Sku == createProductDto.Sku, cancellationToken);
        
        if (existingSku)
        {
            throw new InvalidOperationException("A product with this SKU already exists.");
        }

        var product = _mapper.Map<Product>(createProductDto);
        product.Slug = GenerateSlug(createProductDto.Name);
        
        // Ensure BrandId is set if provided
        if (createProductDto.BrandId.HasValue)
        {
            product.BrandId = createProductDto.BrandId.Value;
        }
        
        await _unitOfWork.Repository<Product>().AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductWithImageAsync(CreateProductWithImageDto createProductDto, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        // Validate image file
        if (imageFile == null || imageFile.Length == 0)
        {
            throw new ArgumentException("Image file is required.");
        }

        var categoryExists = await _unitOfWork.Repository<Category>()
            .AnyAsync(c => c.Id == createProductDto.CategoryId && c.IsActive, cancellationToken);
        
        if (!categoryExists)
        {
            throw new ArgumentException("Category not found or inactive.");
        }

        // Check if product with same name already exists
        var existingProductName = await _unitOfWork.Repository<Product>()
            .AnyAsync(p => p.Name == createProductDto.Name, cancellationToken);
        
        if (existingProductName)
        {
            throw new InvalidOperationException("A product with this name already exists.");
        }

        var existingSku = await _unitOfWork.Repository<Product>()
            .AnyAsync(p => p.Sku == createProductDto.Sku, cancellationToken);
        
        if (existingSku)
        {
            throw new InvalidOperationException("A product with this SKU already exists.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = createProductDto.Name,
            Slug = GenerateSlug(createProductDto.Name),
            Description = createProductDto.Description,
            ShortDescription = createProductDto.ShortDescription,
            Sku = createProductDto.Sku,
            IsHotDeal = createProductDto.IsHotDeal,
            StockQuantity = createProductDto.StockQuantity,
            CategoryId = createProductDto.CategoryId,
            BrandId = createProductDto.BrandId, // Add BrandId
            IsActive = true,
            Price = createProductDto.Price,
            DiscountedPrice = createProductDto.DiscountedPrice,
            CreatedAt = DateTime.UtcNow
        };

        // Upload image
        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "products");
        product.ImageUrl = imageUrl;

        // Add product with all related data
        await _unitOfWork.Repository<Product>().AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get the created product with all relationships for proper mapping
        var createdProduct = await _unitOfWork.Repository<Product>()
            .GetByIdWithIncludesAsync(product.Id, p => p.Category, p => p.Brand);
        
        var productDto = _mapper.Map<ProductDto>(createdProduct);
        
        // Populate brand and category information
        await PopulateCategoryBreadcrumbsForSingleProduct(productDto, cancellationToken);
        
        return productDto;
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "");
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException("Product not found.");
        }

        if (updateProductDto.CategoryId != product.CategoryId)
        {
            var categoryExists = await _unitOfWork.Repository<Category>()
                .AnyAsync(c => c.Id == updateProductDto.CategoryId && c.IsActive, cancellationToken);
            
            if (!categoryExists)
            {
                throw new ArgumentException("Category not found or inactive.");
            }
        }

        // Validate brand if provided
        if (updateProductDto.BrandId.HasValue)
        {
            var brandExists = await _unitOfWork.Repository<Brand>()
                .AnyAsync(b => b.Id == updateProductDto.BrandId.Value && b.IsActive, cancellationToken);
            
            if (!brandExists)
            {
                throw new ArgumentException("Brand not found or inactive.");
            }
        }

        _mapper.Map(updateProductDto, product);
        product.UpdatedAt = DateTime.UtcNow;
        
        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateProductWithImageAsync(Guid id, UpdateProductDto updateProductDto, IFormFile? imageFile, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException("Product not found.");
        }

        if (updateProductDto.CategoryId != product.CategoryId)
        {
            var categoryExists = await _unitOfWork.Repository<Category>()
                .AnyAsync(c => c.Id == updateProductDto.CategoryId && c.IsActive, cancellationToken);
            
            if (!categoryExists)
            {
                throw new ArgumentException("Category not found or inactive.");
            }
        }

        // Validate brand if provided
        if (updateProductDto.BrandId.HasValue)
        {
            var brandExists = await _unitOfWork.Repository<Brand>()
                .AnyAsync(b => b.Id == updateProductDto.BrandId.Value && b.IsActive, cancellationToken);
            
            if (!brandExists)
            {
                throw new ArgumentException("Brand not found or inactive.");
            }
        }

        // Store old image URL before mapping
        var oldImageUrl = product.ImageUrl;
        
        // Map the DTO to product entity
        _mapper.Map(updateProductDto, product);
        product.UpdatedAt = DateTime.UtcNow;

        // Upload new image if provided (after mapping to avoid overwrite)
        if (imageFile != null && imageFile.Length > 0)
        {
            // Upload new image
            var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "products");
            product.ImageUrl = imageUrl;
        }
        
        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return product with all related data
        var updatedProduct = await _unitOfWork.Repository<Product>()
            .GetByIdWithIncludesAsync(id, p => p.Category, p => p.Brand, p => p.Images);
        
        return _mapper.Map<ProductDto>(updatedProduct);
    }

    public async Task<ProductDto> UpdateProductWithFilesAsync(Guid id, UpdateProductDto updateProductDto, IFormFile? imageFile, IFormFile[]? detailImageFiles, IFormFile? pdfFile, Guid? userId, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException("Product not found.");
        }

        if (updateProductDto.CategoryId != product.CategoryId)
        {
            var categoryExists = await _unitOfWork.Repository<Category>()
                .AnyAsync(c => c.Id == updateProductDto.CategoryId && c.IsActive, cancellationToken);
            
            if (!categoryExists)
            {
                throw new ArgumentException("Category not found or inactive.");
            }
        }

        // Validate brand if provided
        if (updateProductDto.BrandId.HasValue)
        {
            var brandExists = await _unitOfWork.Repository<Brand>()
                .AnyAsync(b => b.Id == updateProductDto.BrandId.Value && b.IsActive, cancellationToken);
            
            if (!brandExists)
            {
                throw new ArgumentException("Brand not found or inactive.");
            }
        }

        // Map the DTO to product entity
        _mapper.Map(updateProductDto, product);
        product.UpdatedAt = DateTime.UtcNow;

        // Upload new main image if provided
        if (imageFile != null && imageFile.Length > 0)
        {
            var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "products");
            product.ImageUrl = imageUrl;
        }

        // Handle detail images - upload new ones and manage existing ones
        if (detailImageFiles != null && detailImageFiles.Length > 0)
        {
            // Upload new detail images (add to existing ones, don't replace)
            foreach (var detailImageFile in detailImageFiles)
            {
                if (detailImageFile.Length > 0)
                {
                    var detailImageUrl = await _fileUploadService.UploadFileAsync(detailImageFile, "product-details");
                    
                    var productImage = new ProductImage
                    {
                        Id = Guid.NewGuid(),
                        ProductId = id,
                        ImageUrl = detailImageUrl,
                        IsDetailImage = true,
                        IsPrimary = false,
                        SortOrder = 999, // High sort order to distinguish from normal images
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Repository<ProductImage>().AddAsync(productImage, cancellationToken);
                }
            }
        }

        // Handle existing detail images management (only if DetailImageUrls is provided)
        if (updateProductDto.DetailImageUrls != null)
        {
            var existingDetailImages = await _unitOfWork.Repository<ProductImage>()
                .FindAsync(pi => pi.ProductId == id && pi.IsDetailImage == true, cancellationToken);

            // If DetailImageUrls is empty array, remove all detail images
            if (!updateProductDto.DetailImageUrls.Any())
            {
                // Delete existing detail image files from storage
                foreach (var existingImage in existingDetailImages)
                {
                    if (!string.IsNullOrEmpty(existingImage.ImageUrl))
                    {
                        await _fileUploadService.DeleteFileAsync(existingImage.ImageUrl);
                    }
                }
                // Remove from database
                if (existingDetailImages.Any())
                {
                    _unitOfWork.Repository<ProductImage>().RemoveRange(existingDetailImages);
                }
            }
            else
            {
                // Keep only the detail images specified in DetailImageUrls
                var imagesToKeep = existingDetailImages
                    .Where(img => updateProductDto.DetailImageUrls.Contains(img.ImageUrl))
                    .ToList();
                
                var imagesToDelete = existingDetailImages.Except(imagesToKeep).ToList();
                
                // Delete files that are not in the keep list
                foreach (var imageToDelete in imagesToDelete)
                {
                    if (!string.IsNullOrEmpty(imageToDelete.ImageUrl))
                    {
                        await _fileUploadService.DeleteFileAsync(imageToDelete.ImageUrl);
                    }
                }
                
                if (imagesToDelete.Any())
                {
                    _unitOfWork.Repository<ProductImage>().RemoveRange(imagesToDelete);
                }
            }
        }

        // Handle PDF file upload/update
        if (pdfFile != null && pdfFile.Length > 0)
        {
            // Validate file is PDF
            if (pdfFile.ContentType != "application/pdf")
            {
                throw new ArgumentException("Only PDF files are allowed");
            }

            // Check if product already has a PDF
            var existingPdf = await _unitOfWork.Repository<ProductPdf>()
                .FirstOrDefaultAsync(p => p.ProductId == id, cancellationToken);
            
            if (existingPdf != null)
            {
                // Delete the old PDF file
                await _fileUploadService.DeleteFileAsync(existingPdf.FilePath);
                
                // Upload new PDF file
                var newPdfPath = await _fileUploadService.UploadFileAsync(pdfFile, "product-pdfs");
                
                // Update existing PDF record
                existingPdf.FileName = Path.GetFileName(newPdfPath);
                existingPdf.OriginalFileName = pdfFile.FileName;
                existingPdf.FilePath = newPdfPath;
                existingPdf.ContentType = pdfFile.ContentType;
                existingPdf.FileSize = pdfFile.Length;
                existingPdf.UpdatedAt = DateTime.UtcNow;
                existingPdf.UpdatedBy = userId;
                
                _unitOfWork.Repository<ProductPdf>().Update(existingPdf);
            }
            else
            {
                // Create new PDF record
                var pdfPath = await _fileUploadService.UploadFileAsync(pdfFile, "product-pdfs");
                
                var productPdf = new ProductPdf
                {
                    Id = Guid.NewGuid(),
                    ProductId = id,
                    FileName = Path.GetFileName(pdfPath),
                    OriginalFileName = pdfFile.FileName,
                    FilePath = pdfPath,
                    ContentType = pdfFile.ContentType,
                    FileSize = pdfFile.Length,
                    IsActive = true,
                    CreatedBy = userId ?? Guid.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<ProductPdf>().AddAsync(productPdf, cancellationToken);
            }
        }
        

        
        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return product with all related data
        var updatedProduct = await _unitOfWork.Repository<Product>()
            .GetByIdWithIncludesAsync(id, p => p.Category, p => p.Brand, p => p.Images);
        
        return _mapper.Map<ProductDto>(updatedProduct);
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return false;
        }

        var inCart = await _unitOfWork.Repository<CartItem>()
            .AnyAsync(ci => ci.ProductId == id, cancellationToken);
        
        if (inCart)
        {
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Product>().Update(product);
        }
        else
        {
            _unitOfWork.Repository<Product>().Remove(product);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }



    public async Task<IEnumerable<ProductListDto>> SearchProductsAsync(string searchTerm, UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        var searchResults = products.Where(p => p.IsActive && 
                           (p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                            p.Sku.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(searchResults);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return productDtos;
    }

    public async Task<GlobalSearchResultDto> GlobalSearchAsync(string searchTerm, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        // Search products (by name and SKU only)
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        var productResults = products.Where(p => p.IsActive && 
                           (p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                            p.Sku.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(productResults);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);

        // Search categories (by name only)
        var allCategories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
        var categoryResults = allCategories.Where(c => c.IsActive && 
                           c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        // Search brands (by name only)
        var allBrands = await _brandService.GetAllBrandsAsync(cancellationToken);
        var brandResults = allBrands.Where(b => b.IsActive && 
                           b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        return new GlobalSearchResultDto
        {
            Categories = categoryResults,
            Brands = brandResults,
            Products = productDtos
        };
    }



    public async Task<ProductDto> UploadProductImageAsync(Guid productId, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException("Product not found");
        }

        // IMPORTANT: Do not delete old images to preserve them after publish
        // if (!string.IsNullOrEmpty(product.ImageUrl))
        // {
        //     await _fileUploadService.DeleteFileAsync(product.ImageUrl);
        // }

        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "products");
        product.ImageUrl = imageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UploadProductImagesAsync(Guid productId, IFormFileCollection imageFiles, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException("Product not found");
        }

        // Upload multiple images as detail images (append to existing, don't replace)
        var imageUrls = await _fileUploadService.UploadMultipleFilesAsync(imageFiles, "product-details");
        
        if (imageUrls.Any())
        {
            // Get existing detail images to calculate proper sort order
            var existingDetailImages = await _unitOfWork.Repository<ProductImage>()
                .FindAsync(pi => pi.ProductId == productId && pi.IsDetailImage == true, cancellationToken);
            
            // Calculate the next sort order starting from the highest existing detail image sort order
            // If no existing detail images, start from 999 (following the pattern used elsewhere for detail images)
            int nextSortOrder = existingDetailImages.Any() 
                ? existingDetailImages.Max(img => img.SortOrder) + 1 
                : 999;

            // IMPORTANT: Do NOT remove existing images - only append new detail images
            // This preserves the main product image (Product.ImageUrl) and all existing ProductImage records

            product.UpdatedAt = DateTime.UtcNow;

            // Add new detail images without removing existing ones
            for (int i = 0; i < imageUrls.Count; i++)
            {
                var productImage = new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    ImageUrl = imageUrls[i],
                    IsDetailImage = true,  // Mark as detail image
                    IsPrimary = false,     // Never set as primary (main image is stored in Product.ImageUrl)
                    SortOrder = nextSortOrder + i,  // Sequential sort order after existing detail images
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<ProductImage>().AddAsync(productImage, cancellationToken);
            }

            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<bool> DeleteProductImageAsync(Guid productId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            return false;
        }

        var deleted = await _fileUploadService.DeleteFileAsync(imageUrl);
        
        if (deleted && product.ImageUrl == imageUrl)
        {
            product.ImageUrl = null;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return deleted;
    }

    public async Task<bool> DeleteProductDetailImageAsync(Guid productId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            return false;
        }

        // Normalize the imageUrl - decode URL encoding, trim whitespace and ensure consistent format
        var normalizedUrl = imageUrl?.Trim();
        if (string.IsNullOrEmpty(normalizedUrl))
        {
            return false;
        }

        // URL decode in case the parameter was encoded
        try
        {
            normalizedUrl = WebUtility.UrlDecode(normalizedUrl);
        }
        catch
        {
            // If decoding fails, use original string
        }

        // Normalize URL - ensure it starts with / if it's a relative path
        if (!normalizedUrl.StartsWith("/") && normalizedUrl.StartsWith("uploads"))
        {
            normalizedUrl = "/" + normalizedUrl;
        }

        // Get all images for this product first
        var allImages = await _unitOfWork.Repository<ProductImage>()
            .FindAsync(pi => pi.ProductId == productId, cancellationToken);

        // Try multiple matching strategies
        ProductImage? detailImage = null;

        // Strategy 1: Exact match on all images
        detailImage = allImages.FirstOrDefault(pi => 
            pi.ImageUrl == normalizedUrl || 
            pi.ImageUrl.Equals(normalizedUrl, StringComparison.OrdinalIgnoreCase));

        // Strategy 2: Case-insensitive match
        if (detailImage == null)
        {
            detailImage = allImages.FirstOrDefault(pi => 
                string.Equals(pi.ImageUrl, normalizedUrl, StringComparison.OrdinalIgnoreCase));
        }

        // Strategy 3: Match by filename only (in case path differs slightly)
        if (detailImage == null)
        {
            var fileName = Path.GetFileName(normalizedUrl);
            if (!string.IsNullOrEmpty(fileName))
            {
                detailImage = allImages.FirstOrDefault(pi => 
                    !string.IsNullOrEmpty(pi.ImageUrl) && 
                    Path.GetFileName(pi.ImageUrl) == fileName);
            }
        }

        // Strategy 4: Contains match (in case of partial URL match)
        if (detailImage == null)
        {
            detailImage = allImages.FirstOrDefault(pi => 
                (!string.IsNullOrEmpty(pi.ImageUrl) && pi.ImageUrl.Contains(normalizedUrl, StringComparison.OrdinalIgnoreCase)) ||
                normalizedUrl.Contains(pi.ImageUrl, StringComparison.OrdinalIgnoreCase));
        }

        // Strategy 5: Match by image ID if the URL contains the image ID
        if (detailImage == null)
        {
            // Try to extract potential image ID from URL patterns
            foreach (var img in allImages)
            {
                var imgFileName = Path.GetFileNameWithoutExtension(img.ImageUrl ?? "");
                var normalizedFileName = Path.GetFileNameWithoutExtension(normalizedUrl);
                
                // If filenames match (without extension), it's likely the same image
                if (!string.IsNullOrEmpty(imgFileName) && 
                    !string.IsNullOrEmpty(normalizedFileName) &&
                    imgFileName.Equals(normalizedFileName, StringComparison.OrdinalIgnoreCase))
                {
                    detailImage = img;
                    break;
                }
            }
        }

        if (detailImage == null)
        {
            return false;
        }

        // Use the actual URL from database for file deletion
        var actualImageUrl = detailImage.ImageUrl;

        // Delete file from storage
        var deleted = await _fileUploadService.DeleteFileAsync(actualImageUrl);
        
        if (deleted)
        {
            // Remove from database
            _unitOfWork.Repository<ProductImage>().Remove(detailImage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return deleted;
    }

    public async Task<bool> DeleteProductDetailImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        try
        {
            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Starting deletion for ImageId: {imageId}");

            // Find the image by ID
            var detailImage = await _unitOfWork.Repository<ProductImage>()
                .GetByIdAsync(imageId, cancellationToken);

            if (detailImage == null)
            {
                System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Image NOT FOUND in database for ImageId: {imageId}");
                
                // Try to find by FirstOrDefault to double check
                var alternativeImage = await _unitOfWork.Repository<ProductImage>()
                    .FirstOrDefaultAsync(pi => pi.Id == imageId, cancellationToken);
                
                if (alternativeImage == null)
                {
                    System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Image also NOT FOUND with FirstOrDefaultAsync for ImageId: {imageId}");
                    
                    // Get all images to see what exists
                    var allImages = await _unitOfWork.Repository<ProductImage>()
                        .GetAllAsync(cancellationToken);
                    System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Total images in database: {allImages.Count()}");
                    
                    // Get images for debugging - first 10
                    foreach (ProductImage img in allImages.Take(10))
                    {
                        System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Found image - Id: {img.Id}, ProductId: {img.ProductId}, ImageUrl: {img.ImageUrl}, IsDetailImage: {img.IsDetailImage}");
                    }
                    
                    return false;
                }
                else
                {
                    System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Image FOUND with FirstOrDefaultAsync - ImageUrl: {alternativeImage.ImageUrl}");
                    detailImage = alternativeImage;
                }
            }
            else
            {
                System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Image FOUND - ProductId: {detailImage.ProductId}, ImageUrl: {detailImage.ImageUrl}, IsDetailImage: {detailImage.IsDetailImage}");
            }

            // Use the actual URL from database for file deletion
            var actualImageUrl = detailImage.ImageUrl;
            var productId = detailImage.ProductId;
            var isPrimary = detailImage.IsPrimary;
            
            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Image details - ProductId: {productId}, IsPrimary: {isPrimary}, ImageUrl: {actualImageUrl}");
            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Attempting to delete file: {actualImageUrl}");

            // If this is a primary image, handle it appropriately
            if (isPrimary)
            {
                System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] This is a primary image. IsDetailImage: {detailImage.IsDetailImage}");
                
                // For detail images, allow deletion without restrictions
                if (detailImage.IsDetailImage)
                {
                    System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] This is a detail image primary - can be deleted without restrictions");
                    // Detail image primary can always be deleted
                }
                else
                {
                    // For normal images (non-detail), check if it's the last image
                    System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] This is a normal (non-detail) primary image, checking if it can be deleted...");
                    
                    // Get all remaining images (excluding the one being deleted)
                    var remainingImages = await _unitOfWork.Repository<ProductImage>()
                        .FindAsync(pi => pi.ProductId == productId && pi.Id != imageId, cancellationToken);
                    
                    var remainingNormalImages = remainingImages
                        .Where(img => !img.IsDetailImage)
                        .ToList();
                    
                    // If this is the last normal image (non-detail), don't allow deletion
                    if (!remainingNormalImages.Any())
                    {
                        System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Cannot delete primary image - it's the last normal image for this product");
                        throw new InvalidOperationException("Cannot delete the last primary image. Product must have at least one normal image.");
                    }
                    
                    System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Primary normal image can be deleted. Remaining normal images: {remainingNormalImages.Count}");
                    
                    var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
                    
                    if (product != null)
                    {
                        // If this image URL matches Product.ImageUrl, clear it (will be set to new primary below)
                        if (product.ImageUrl == actualImageUrl)
                        {
                            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Clearing Product.ImageUrl as it matches deleted image");
                            product.ImageUrl = null;
                        }
                        
                        // Set the next normal image as primary (by SortOrder)
                        var nextPrimaryImage = remainingNormalImages
                            .OrderBy(img => img.SortOrder)
                            .FirstOrDefault();
                        
                        if (nextPrimaryImage != null)
                        {
                            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Setting new primary image: {nextPrimaryImage.Id} ({nextPrimaryImage.ImageUrl})");
                            nextPrimaryImage.IsPrimary = true;
                            nextPrimaryImage.SortOrder = 1;
                            
                            // Update Product.ImageUrl to the new primary image
                            product.ImageUrl = nextPrimaryImage.ImageUrl;
                            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Updated Product.ImageUrl to new primary: {nextPrimaryImage.ImageUrl}");
                            
                            // Update sort orders of remaining normal images
                            var otherImages = remainingNormalImages
                                .Where(img => img.Id != nextPrimaryImage.Id)
                                .OrderBy(img => img.SortOrder)
                                .ToList();
                            
                            for (int i = 0; i < otherImages.Count; i++)
                            {
                                otherImages[i].SortOrder = i + 2; // Start from 2, as 1 is primary
                            }
                            
                            _unitOfWork.Repository<Product>().Update(product);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Product not found: {productId}");
                    }
                }
            }
            
            // Delete file from storage
            var deleted = await _fileUploadService.DeleteFileAsync(actualImageUrl);
            
            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] File deletion result: {deleted}");
            
            // Remove from database regardless of file deletion result
            // This handles cases where file doesn't exist but record is in database (orphaned records)
            _unitOfWork.Repository<ProductImage>().Remove(detailImage);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] Image removed from database successfully");
            
            // Return true if file was deleted, or if it didn't exist (orphaned record cleanup)
            // We consider it successful if we removed the database record
            return true;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] EXCEPTION: {ex.Message}");
            System.Console.WriteLine($"[DeleteProductDetailImageByIdAsync] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<IEnumerable<ProductStockDto>> GetProductStockStatusAsync(CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category);
        
        var productStockDtos = products.Select(product => new ProductStockDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            StockQuantity = product.StockQuantity,
            StockStatus = GetStockStatus(product.StockQuantity),
            StockStatusText = GetStockStatusText(product.StockQuantity),
            CategoryName = product.Category?.Name ?? "Unknown",
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt
        }).OrderBy(p => p.StockStatus).ThenBy(p => p.StockQuantity).ToList();

        return productStockDtos;
    }

    public async Task<StockSummaryDto> GetStockSummaryAsync(CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var productsList = products.ToList();

        var summary = new StockSummaryDto
        {
            TotalProducts = productsList.Count,
            InStockProducts = productsList.Count(p => p.IsActive && GetStockStatus(p.StockQuantity) == StockStatus.InStock),
            LowStockProducts = productsList.Count(p => p.IsActive && GetStockStatus(p.StockQuantity) == StockStatus.LowStock),
            OutOfStockProducts = productsList.Count(p => p.IsActive && GetStockStatus(p.StockQuantity) == StockStatus.OutOfStock),
            InactiveProducts = productsList.Count(p => !p.IsActive)
        };

        return summary;
    }



    public async Task<RecommendedProductsDto> GetRecommendedProductsAsync(RecommendationRequestDto? request = null, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        request ??= new RecommendationRequestDto();
        var limit = request.Limit ?? 10;
        
        var recommendations = new RecommendedProductsDto();
        
        // Get all active products with includes
        var allProducts = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Images);
        var activeProducts = allProducts.Where(p => p.IsActive).ToList();

        // 1. Based on user favorites (if user is authenticated)
        if (userId.HasValue)
        {
            var userFavorites = await _unitOfWork.Repository<UserFavorite>().GetAllAsync(cancellationToken);
            var userFavoriteProductIds = userFavorites.Where(f => f.UserId == userId.Value).Select(f => f.ProductId).ToList();
            
            if (userFavoriteProductIds.Any())
            {
                // Get categories of favorite products
                var favoriteProducts = activeProducts.Where(p => userFavoriteProductIds.Contains(p.Id)).ToList();
                var favoriteCategoryIds = favoriteProducts.Select(p => p.CategoryId).Distinct().ToList();
                
                // Recommend products from same categories (excluding already favorited)
                var basedOnFavorites = activeProducts
                    .Where(p => favoriteCategoryIds.Contains(p.CategoryId) && !userFavoriteProductIds.Contains(p.Id))
                    .OrderBy(x => Guid.NewGuid())
                    .Take(limit)
                    .ToList();
                
                recommendations.BasedOnFavorites = _mapper.Map<IEnumerable<ProductListDto>>(basedOnFavorites);
            }
        }

        // 2. Based on category (if specified)
        if (request.CategoryId.HasValue)
        {
            var categoryProducts = activeProducts
                .Where(p => p.CategoryId == request.CategoryId.Value)
                .OrderBy(x => Guid.NewGuid())
                .Take(limit)
                .ToList();
                
            recommendations.BasedOnCategory = _mapper.Map<IEnumerable<ProductListDto>>(categoryProducts);
        }

        // 3. Hot deals
        var hotDeals = activeProducts
            .Where(p => p.IsHotDeal)
            .OrderBy(x => Guid.NewGuid())
            .Take(limit)
            .ToList();
            
        recommendations.HotDeals = _mapper.Map<IEnumerable<ProductListDto>>(hotDeals);

        // 4. Recently added products
        var recentlyAdded = activeProducts
            .OrderBy(x => Guid.NewGuid())
            .Take(limit)
            .ToList();
            
        recommendations.RecentlyAdded = _mapper.Map<IEnumerable<ProductListDto>>(recentlyAdded);

        // 5. Similar products (if specific product is provided)
        if (request.ProductId.HasValue)
        {
            var targetProduct = activeProducts.FirstOrDefault(p => p.Id == request.ProductId.Value);
            if (targetProduct != null)
            {
                // Find products in same category, excluding the target product
                var similarProducts = activeProducts
                    .Where(p => p.CategoryId == targetProduct.CategoryId && p.Id != targetProduct.Id)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(limit)
                    .ToList();
                    
                recommendations.SimilarProducts = _mapper.Map<IEnumerable<ProductListDto>>(similarProducts);
            }
        }

        return recommendations;
    }



    private static StockStatus GetStockStatus(int stockQuantity)
    {
        return stockQuantity switch
        {
            0 => StockStatus.OutOfStock,
            > 0 and <= 10 => StockStatus.LowStock,
            > 10 => StockStatus.InStock,
            _ => StockStatus.OutOfStock
        };
    }

    private static string GetStockStatusText(int stockQuantity)
    {
        return stockQuantity switch
        {
            0 => "Out of Stock",
            > 0 and <= 10 => "Low Stock",
            > 10 => "In Stock",
            _ => "Out of Stock"
        };
    }

    public async Task<ProductSpecificationDto?> GetProductSpecificationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdWithIncludesAsync(id, p => p.Category, p => p.Specifications);
        
        if (product == null)
        {
            return null;
        }

        var specifications = new ProductSpecificationDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductSku = product.Sku,
            SpecificationGroups = new List<SpecificationGroupDto>()
        };

        // If no custom specifications exist, generate default ones
        if (!product.Specifications.Any())
        {
            GenerateProductSpecifications(product, specifications);
        }
        else
        {
            // Use stored specifications from database
            var groupedSpecs = product.Specifications
                .OrderBy(s => s.SortOrder)
                .GroupBy(s => s.GroupName)
                .ToList();

            foreach (var group in groupedSpecs)
            {
                var specGroup = new SpecificationGroupDto
                {
                    GroupName = group.Key,
                    Items = group.Select(spec => new SpecificationItemDto
                    {
                        Name = spec.Name,
                        Value = spec.Value,
                        Unit = spec.Unit,
                        Type = spec.Type
                    }).ToList()
                };
                specifications.SpecificationGroups.Add(specGroup);
            }
        }

        return specifications;
    }

    private static void GenerateProductSpecifications(Product product, ProductSpecificationDto specifications)
    {
        // Basic Information Group
        var basicInfoGroup = new SpecificationGroupDto
        {
            GroupName = "Basic Information",
            Items = new List<SpecificationItemDto>
            {
                new() { Name = "SKU", Value = product.Sku, Type = SpecificationType.Text },
                new() { Name = "Category", Value = product.Category?.Name ?? "Unknown", Type = SpecificationType.Text },
                new() { Name = "Stock Status", Value = GetStockStatusText(product.StockQuantity), Type = SpecificationType.Feature },
                new() { Name = "Availability", Value = product.IsActive ? "Available" : "Not Available", Type = SpecificationType.Feature }
            }
        };
        specifications.SpecificationGroups.Add(basicInfoGroup);

        // Generate category-specific specifications
        GenerateCategorySpecificSpecifications(product, specifications);

        // Stock Information Group
        var stockGroup = new SpecificationGroupDto
        {
            GroupName = "Stock Information",
            Items = new List<SpecificationItemDto>
            {
                new() { Name = "Quantity", Value = product.StockQuantity.ToString(), Unit = "units", Type = SpecificationType.Technical },
                new() { Name = "Hot Deal", Value = product.IsHotDeal ? "Yes" : "No", Type = SpecificationType.Feature }
            }
        };
        specifications.SpecificationGroups.Add(stockGroup);
    }

    private static void GenerateCategorySpecificSpecifications(Product product, ProductSpecificationDto specifications)
    {
        var categoryName = product.Category?.Name?.ToLower() ?? "";

        // Computer/Laptop specifications
        if (categoryName.Contains("komputer") || categoryName.Contains("noutbuk"))
        {
            var techSpecs = new SpecificationGroupDto
            {
                GroupName = "Technical Specifications",
                Items = new List<SpecificationItemDto>()
            };

            // Add sample specifications based on product name analysis
            var productName = product.Name.ToLower();
            
            // RAM detection
            if (productName.Contains("16gb") || productName.Contains("16 gb"))
                techSpecs.Items.Add(new() { Name = "RAM", Value = "16GB", Type = SpecificationType.Technical });
            else if (productName.Contains("8gb") || productName.Contains("8 gb"))
                techSpecs.Items.Add(new() { Name = "RAM", Value = "8GB", Type = SpecificationType.Technical });
            else if (productName.Contains("32gb") || productName.Contains("32 gb"))
                techSpecs.Items.Add(new() { Name = "RAM", Value = "32GB", Type = SpecificationType.Technical });

            // Processor detection
            if (productName.Contains("intel") && productName.Contains("i7"))
                techSpecs.Items.Add(new() { Name = "Processor", Value = "Intel® Core™ i7", Type = SpecificationType.Technical });
            else if (productName.Contains("intel") && productName.Contains("i5"))
                techSpecs.Items.Add(new() { Name = "Processor", Value = "Intel® Core™ i5", Type = SpecificationType.Technical });
            else if (productName.Contains("amd"))
                techSpecs.Items.Add(new() { Name = "Processor", Value = "AMD Processor", Type = SpecificationType.Technical });

            // Storage detection
            if (productName.Contains("ssd"))
            {
                if (productName.Contains("512gb") || productName.Contains("512 gb"))
                    techSpecs.Items.Add(new() { Name = "Storage", Value = "512GB SSD", Type = SpecificationType.Technical });
                else if (productName.Contains("256gb") || productName.Contains("256 gb"))
                    techSpecs.Items.Add(new() { Name = "Storage", Value = "256GB SSD", Type = SpecificationType.Technical });
                else if (productName.Contains("1tb") || productName.Contains("1 tb"))
                    techSpecs.Items.Add(new() { Name = "Storage", Value = "1TB SSD", Type = SpecificationType.Technical });
            }

            // Color detection
            if (productName.Contains("gray") || productName.Contains("grey"))
                techSpecs.Items.Add(new() { Name = "Color", Value = "Gray", Type = SpecificationType.Color });
            else if (productName.Contains("black"))
                techSpecs.Items.Add(new() { Name = "Color", Value = "Black", Type = SpecificationType.Color });
            else if (productName.Contains("white"))
                techSpecs.Items.Add(new() { Name = "Color", Value = "White", Type = SpecificationType.Color });
            else if (productName.Contains("silver"))
                techSpecs.Items.Add(new() { Name = "Color", Value = "Silver", Type = SpecificationType.Color });

            if (techSpecs.Items.Any())
                specifications.SpecificationGroups.Add(techSpecs);
        }

        // Camera/Surveillance specifications
        if (categoryName.Contains("kamera") || categoryName.Contains("musahide"))
        {
            var cameraSpecs = new SpecificationGroupDto
            {
                GroupName = "Camera Specifications",
                Items = new List<SpecificationItemDto>()
            };

            var productName = product.Name.ToLower();
            
            // Resolution detection
            if (productName.Contains("4k"))
                cameraSpecs.Items.Add(new() { Name = "Resolution", Value = "4K", Type = SpecificationType.Technical });
            else if (productName.Contains("1080p") || productName.Contains("full hd"))
                cameraSpecs.Items.Add(new() { Name = "Resolution", Value = "1080p Full HD", Type = SpecificationType.Technical });
            else if (productName.Contains("720p") || productName.Contains("hd"))
                cameraSpecs.Items.Add(new() { Name = "Resolution", Value = "720p HD", Type = SpecificationType.Technical });

            // Connection type
            if (productName.Contains("wifi") || productName.Contains("wireless"))
                cameraSpecs.Items.Add(new() { Name = "Connection", Value = "WiFi/Wireless", Type = SpecificationType.Feature });
            else if (productName.Contains("ip"))
                cameraSpecs.Items.Add(new() { Name = "Connection", Value = "IP Network", Type = SpecificationType.Feature });

            if (cameraSpecs.Items.Any())
                specifications.SpecificationGroups.Add(cameraSpecs);
        }

        // Add more category-specific logic as needed
    }

    public async Task<ProductSpecificationDto> CreateProductSpecificationsAsync(CreateProductSpecificationDto createDto, CancellationToken cancellationToken = default)
    {
        // Verify product exists
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(createDto.ProductId, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException($"Product with ID {createDto.ProductId} not found.");
        }

        // Delete existing specifications for this product
        var existingSpecs = await _unitOfWork.Repository<ProductSpecification>()
            .FindAsync(s => s.ProductId == createDto.ProductId, cancellationToken);
        
        if (existingSpecs.Any())
        {
            _unitOfWork.Repository<ProductSpecification>().RemoveRange(existingSpecs);
        }

        // Create new specifications
        var sortOrder = 0;
        foreach (var group in createDto.SpecificationGroups)
        {
            foreach (var item in group.Items)
            {
                var specification = new ProductSpecification
                {
                    Id = Guid.NewGuid(),
                    ProductId = createDto.ProductId,
                    GroupName = group.GroupName,
                    Name = item.Name,
                    Value = item.Value,
                    Unit = item.Unit,
                    Type = item.Type,
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<ProductSpecification>().AddAsync(specification, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return the created specifications
        var result = await GetProductSpecificationsAsync(createDto.ProductId, cancellationToken);
        return result!;
    }

    public async Task<ProductSpecificationDto> UpdateProductSpecificationsAsync(Guid productId, UpdateProductSpecificationDto updateDto, CancellationToken cancellationToken = default)
    {
        // Verify product exists
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException($"Product with ID {productId} not found.");
        }

        // Delete existing specifications for this product
        var existingSpecs = await _unitOfWork.Repository<ProductSpecification>()
            .FindAsync(s => s.ProductId == productId, cancellationToken);
        
        if (existingSpecs.Any())
        {
            _unitOfWork.Repository<ProductSpecification>().RemoveRange(existingSpecs);
        }

        // Create new specifications
        var sortOrder = 0;
        foreach (var group in updateDto.SpecificationGroups)
        {
            foreach (var item in group.Items)
            {
                var specification = new ProductSpecification
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    GroupName = group.GroupName,
                    Name = item.Name,
                    Value = item.Value,
                    Unit = item.Unit,
                    Type = item.Type,
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<ProductSpecification>().AddAsync(specification, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return the updated specifications
        var result = await GetProductSpecificationsAsync(productId, cancellationToken);
        return result!;
    }

    public async Task<bool> DeleteProductSpecificationsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Get all specifications for this product
        var existingSpecs = await _unitOfWork.Repository<ProductSpecification>()
            .FindAsync(s => s.ProductId == productId, cancellationToken);

        if (!existingSpecs.Any())
        {
            return false;
        }

        // Delete all specifications for this product
        _unitOfWork.Repository<ProductSpecification>().RemoveRange(existingSpecs);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FilteredProductsResultDto> GetFilteredProductsAsync(ProductFilterCriteriaDto criteria, UserRole? userRole = null, CancellationToken cancellationToken = default)
    {
        if (criteria.Page <= 0) criteria.Page = 1;
        if (criteria.PageSize <= 0) criteria.PageSize = 20;
        
        var query = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Images, p => p.Brand);
        var products = query.Where(p => p.IsActive).AsQueryable();
        
        System.Console.WriteLine($"DEBUG: Total active products in database: {products.Count()}");

        if (!string.IsNullOrWhiteSpace(criteria.BrandSlug))
        {
            var brand = await _unitOfWork.Repository<Brand>()
                .FirstOrDefaultAsync(b => b.Slug == criteria.BrandSlug && b.IsActive, cancellationToken);
            if (brand != null)
            {
                products = products.Where(p => p.BrandId == brand.Id);
                System.Console.WriteLine($"DEBUG: Products after brand filter ({criteria.BrandSlug}): {products.Count()}");
            }
        }

        if (criteria.IsHotDeal.HasValue)
        {
            products = products.Where(p => p.IsHotDeal == criteria.IsHotDeal.Value);
            System.Console.WriteLine($"DEBUG: Products after hot deal filter: {products.Count()}");
        }

        if (criteria.IsRecommended.HasValue && criteria.IsRecommended.Value)
        {
            var recentDate = DateTime.UtcNow.AddDays(-30);
            products = products.Where(p => p.CreatedAt >= recentDate).OrderByDescending(p => p.CreatedAt);
            System.Console.WriteLine($"DEBUG: Products after recommended filter (last 30 days): {products.Count()}");
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.ToLower().Trim();
            products = products.Where(p => p.Name.ToLower().Contains(searchTerm));
            System.Console.WriteLine($"DEBUG: Products after search term filter ('{criteria.SearchTerm}'): {products.Count()}");
        }

        if (criteria.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryIdsIncludingSubcategories(criteria.CategoryId.Value, cancellationToken);
            
            System.Console.WriteLine($"DEBUG: Category filter for {criteria.CategoryId.Value}");
            System.Console.WriteLine($"DEBUG: Found category IDs: {string.Join(", ", categoryIds)}");
            System.Console.WriteLine($"DEBUG: Total products before category filter: {products.Count()}");
            
            var productsList = products.ToList();
            System.Console.WriteLine($"DEBUG: Products converted to list, count: {productsList.Count}");
            
            var filteredProducts = productsList.Where(p => categoryIds.Contains(p.CategoryId)).ToList();
            System.Console.WriteLine($"DEBUG: Products after category filter: {filteredProducts.Count()}");
            
            System.Console.WriteLine($"DEBUG: Showing first 10 products and their categories:");
            foreach (var product in productsList.Take(10))
            {
                var isInTargetCategories = categoryIds.Contains(product.CategoryId);
                System.Console.WriteLine($"DEBUG: Product '{product.Name}' has CategoryId: {product.CategoryId} - Matches filter: {isInTargetCategories}");
            }
            
            var categoryDistribution = productsList.GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToList();
            System.Console.WriteLine($"DEBUG: Product distribution by category:");
            foreach (var dist in categoryDistribution.Take(10))
            {
                var isTargetCategory = categoryIds.Contains(dist.CategoryId);
                System.Console.WriteLine($"DEBUG: CategoryId {dist.CategoryId}: {dist.Count} products - Is target: {isTargetCategory}");
            }
            
            products = filteredProducts.AsQueryable();
            
            System.Console.WriteLine($"DEBUG: Total products after category filter: {products.Count()}");
        }

        var hasValidMinPrice = criteria.MinPrice.HasValue && criteria.MinPrice.Value > 0;
        var hasValidMaxPrice = criteria.MaxPrice.HasValue && criteria.MaxPrice.Value > 0;
        
        if (hasValidMinPrice || hasValidMaxPrice)
        {
            System.Console.WriteLine($"DEBUG: Applying price filter - Min: {criteria.MinPrice}, Max: {criteria.MaxPrice}");
            products = products.Where(p => 
                (!hasValidMinPrice || (p.DiscountedPrice ?? p.Price) >= criteria.MinPrice.Value) &&
                (!hasValidMaxPrice || (p.DiscountedPrice ?? p.Price) <= criteria.MaxPrice.Value));
            System.Console.WriteLine($"DEBUG: Products after price filter: {products.Count()}");
        }

        // Apply custom filters
        System.Console.WriteLine($"DEBUG: Applying custom filters, criteria count: {criteria.FilterCriteria.Count}");
        var productIdsBeforeCustomFilters = products.Select(p => p.Id).ToList();
        var filteredProductIds = await ApplyCustomFilters(productIdsBeforeCustomFilters, criteria.FilterCriteria, cancellationToken);
        System.Console.WriteLine($"DEBUG: Products before custom filters: {productIdsBeforeCustomFilters.Count}");
        System.Console.WriteLine($"DEBUG: Products after custom filters: {filteredProductIds.Count}");
        
        var filteredProductsList = products.ToList().Where(p => filteredProductIds.Contains(p.Id)).ToList();
        products = filteredProductsList.AsQueryable();

        // Get total count before pagination
        var totalCount = products.Count();

        // Convert all filtered products to DTOs
        var allProductDtos = _mapper.Map<IEnumerable<ProductListDto>>(products.ToList());
        
        // Apply sorting
        var sortedProducts = criteria.SortBy?.ToLower() switch
        {
            "price" => criteria.SortOrder == "desc" 
                ? allProductDtos.OrderByDescending(p => p.DiscountedPrice ?? p.Price)
                : allProductDtos.OrderBy(p => p.DiscountedPrice ?? p.Price),
            "createdat" => criteria.SortOrder == "desc" 
                ? allProductDtos.OrderByDescending(p => p.Name) // Defaulting to Name if CreatedAt missing in DTO, or add CreatedAt to DTO
                : allProductDtos.OrderBy(p => p.Name),
            _ => criteria.SortOrder == "desc" 
                ? allProductDtos.OrderByDescending(p => p.Name)
                : allProductDtos.OrderBy(p => p.Name)
        };

        // Apply pagination after sorting
        var pagedProducts = sortedProducts
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();
        await PopulateCategoryBreadcrumbs(pagedProducts, cancellationToken);

        // Get applied filters for response
        var appliedFilters = await GetAppliedFilters(criteria.FilterCriteria, cancellationToken);

        return new FilteredProductsResultDto
        {
            Products = pagedProducts,
            TotalCount = totalCount,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize),
            HasNextPage = criteria.Page * criteria.PageSize < totalCount,
            HasPreviousPage = criteria.Page > 1,
            AppliedFilters = appliedFilters
        };
    }

    private async Task<List<Guid>> ApplyCustomFilters(List<Guid> productIds, List<FilterCriteriaDto> filterCriteria, CancellationToken cancellationToken)
    {
        if (!filterCriteria.Any())
        {
            System.Console.WriteLine("DEBUG: No filter criteria provided, returning all product IDs");
            return productIds;
        }

        var filteredProductIds = productIds.ToList();
        System.Console.WriteLine($"DEBUG: Starting custom filters with {filteredProductIds.Count} products");

        foreach (var criteria in filterCriteria)
        {
            System.Console.WriteLine($"DEBUG: Processing filter criteria - FilterId: {criteria.FilterId}");
            System.Console.WriteLine($"DEBUG: FilterOptionIds count: {criteria.FilterOptionIds.Count}");
            System.Console.WriteLine($"DEBUG: CustomValue: '{criteria.CustomValue}'");
            System.Console.WriteLine($"DEBUG: MinValue: {criteria.MinValue}, MaxValue: {criteria.MaxValue}");
            
            var attributeRepository = _unitOfWork.Repository<ProductAttributeValue>();
            
            // Skip invalid filter criteria - check for common placeholder/invalid values
            var hasValidFilterOptions = criteria.FilterOptionIds.Any() && 
                                      !criteria.FilterOptionIds.All(id => id == Guid.Empty) &&
                                      !criteria.FilterOptionIds.Contains(new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
            
            var hasValidCustomValue = !string.IsNullOrEmpty(criteria.CustomValue) && 
                                    criteria.CustomValue != "string" && 
                                    criteria.CustomValue.Trim().Length > 0;
            
            var hasValidRangeValues = (criteria.MinValue.HasValue && criteria.MinValue.Value > 0) || 
                                    (criteria.MaxValue.HasValue && criteria.MaxValue.Value > 0);
            
            // Also check if the filter ID itself is valid (not a placeholder)
            var hasValidFilterId = criteria.FilterId != Guid.Empty && 
                                 criteria.FilterId != new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6");
            
            if (!hasValidFilterId || (!hasValidFilterOptions && !hasValidCustomValue && !hasValidRangeValues))
            {
                System.Console.WriteLine($"DEBUG: Skipping invalid filter criteria - FilterId: {criteria.FilterId}, HasValidOptions: {hasValidFilterOptions}, HasValidCustomValue: {hasValidCustomValue}, HasValidRangeValues: {hasValidRangeValues}");
                continue;
            }
            
            if (hasValidFilterOptions)
            {
                System.Console.WriteLine("DEBUG: Applying filter option criteria");
                // Filter by specific filter options
                var productsWithFilterOptions = await attributeRepository.FindAsync(
                    a => criteria.FilterOptionIds.Contains(a.FilterOptionId!.Value) && 
                         a.FilterId == criteria.FilterId,
                    cancellationToken);
                
                var productIdsWithOptions = productsWithFilterOptions.Select(a => a.ProductId).Distinct().ToList();
                System.Console.WriteLine($"DEBUG: Found {productIdsWithOptions.Count} products with filter options");
                filteredProductIds = filteredProductIds.Intersect(productIdsWithOptions).ToList();
            }
            else if (hasValidCustomValue)
            {
                System.Console.WriteLine("DEBUG: Applying custom value criteria");
                // Filter by custom value (text filters)
                var productsWithCustomValue = await attributeRepository.FindAsync(
                    a => a.FilterId == criteria.FilterId && 
                         a.CustomValue != null && 
                         a.CustomValue.Contains(criteria.CustomValue),
                    cancellationToken);
                
                var productIdsWithCustomValue = productsWithCustomValue.Select(a => a.ProductId).Distinct().ToList();
                System.Console.WriteLine($"DEBUG: Found {productIdsWithCustomValue.Count} products with custom value");
                filteredProductIds = filteredProductIds.Intersect(productIdsWithCustomValue).ToList();
            }
            else if (hasValidRangeValues)
            {
                System.Console.WriteLine("DEBUG: Applying range value criteria");
                // Filter by range values (for numeric custom values)
                var productsWithRangeValue = await attributeRepository.FindAsync(
                    a => a.FilterId == criteria.FilterId && a.CustomValue != null,
                    cancellationToken);

                var validProducts = new List<Guid>();
                foreach (var attr in productsWithRangeValue)
                {
                    if (decimal.TryParse(attr.CustomValue, out var numericValue))
                    {
                        var matchesMin = !criteria.MinValue.HasValue || criteria.MinValue.Value <= 0 || numericValue >= criteria.MinValue.Value;
                        var matchesMax = !criteria.MaxValue.HasValue || criteria.MaxValue.Value <= 0 || numericValue <= criteria.MaxValue.Value;
                        
                        if (matchesMin && matchesMax)
                        {
                            validProducts.Add(attr.ProductId);
                        }
                    }
                }

                System.Console.WriteLine($"DEBUG: Found {validProducts.Count} products with range values");
                filteredProductIds = filteredProductIds.Intersect(validProducts).ToList();
            }
            
            System.Console.WriteLine($"DEBUG: Products remaining after this filter: {filteredProductIds.Count}");
        }

        return filteredProductIds;
    }

    private async Task<IEnumerable<FilterDto>> GetAppliedFilters(List<FilterCriteriaDto> filterCriteria, CancellationToken cancellationToken)
    {
        var appliedFilters = new List<FilterDto>();
        var filterRepository = _unitOfWork.Repository<Filter>();

        foreach (var criteria in filterCriteria)
        {
            var filter = await filterRepository.GetByIdAsync(criteria.FilterId, cancellationToken);
            if (filter != null)
            {
                var filterDto = _mapper.Map<FilterDto>(filter);
                appliedFilters.Add(filterDto);
            }
        }

        return appliedFilters;
    }

    public async Task<object> TestParentCategoryFilteringAsync(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"=== TESTING PARENT CATEGORY FILTERING FOR {parentCategoryId} ===");
        
        var categoryRepository = _unitOfWork.Repository<Category>();
        var productRepository = _unitOfWork.Repository<Product>();

        // Step 1: Check if parent category exists
        var parentCategory = await categoryRepository.GetByIdAsync(parentCategoryId, cancellationToken);
        if (parentCategory == null)
        {
            return new { Error = "Parent category not found", CategoryId = parentCategoryId };
        }

        System.Console.WriteLine($"Parent Category: {parentCategory.Name} (ID: {parentCategory.Id})");

        // Step 2: Get all subcategories
        var allSubcategoryIds = await GetCategoryIdsIncludingSubcategories(parentCategoryId, cancellationToken);
        System.Console.WriteLine($"All category IDs (parent + subcategories): {string.Join(", ", allSubcategoryIds)}");

        // Step 3: Get all products and check their categories
        var allProducts = await productRepository.GetAllAsync(cancellationToken);
        var activeProducts = allProducts.Where(p => p.IsActive).ToList();
        
        System.Console.WriteLine($"Total products in database: {allProducts.Count()}");
        System.Console.WriteLine($"Active products in database: {activeProducts.Count}");

        // Step 4: Find products in target categories
        var productsInTargetCategories = activeProducts.Where(p => allSubcategoryIds.Contains(p.CategoryId)).ToList();
        System.Console.WriteLine($"Products in target categories: {productsInTargetCategories.Count}");

        // Step 5: Show detailed breakdown
        var breakdown = allSubcategoryIds.Select(catId => {
            var category = categoryRepository.GetByIdAsync(catId, cancellationToken).Result;
            var productsInCategory = activeProducts.Where(p => p.CategoryId == catId).ToList();
            
            return new {
                CategoryId = catId,
                CategoryName = category?.Name ?? "Unknown",
                IsParent = catId == parentCategoryId,
                ProductCount = productsInCategory.Count,
                Products = productsInCategory.Select(p => new { p.Id, p.Name, p.CategoryId }).ToList()
            };
        }).ToList();

        return new
        {
            ParentCategory = new { parentCategory.Id, parentCategory.Name },
            AllCategoryIds = allSubcategoryIds,
            TotalProductsFound = productsInTargetCategories.Count,
            CategoryBreakdown = breakdown,
            SampleProducts = productsInTargetCategories.Take(5).Select(p => new { p.Id, p.Name, p.CategoryId }).ToList()
        };
    }

    public async Task<object> DiagnoseCategoryStructureAsync(Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        var productRepository = _unitOfWork.Repository<Product>();

        // Get all categories
        var allCategories = await categoryRepository.GetAllAsync(cancellationToken);
        var allProducts = await productRepository.GetAllAsync(cancellationToken);

        var result = new
        {
            TotalCategories = allCategories.Count(),
            TotalProducts = allProducts.Count(),
            ActiveProducts = allProducts.Count(p => p.IsActive),
            Categories = allCategories.Select(c => new
            {
                c.Id,
                c.Name,
                c.ParentCategoryId,
                IsParent = !c.ParentCategoryId.HasValue,
                ProductCount = allProducts.Count(p => p.CategoryId == c.Id),
                ActiveProductCount = allProducts.Count(p => p.CategoryId == c.Id && p.IsActive)
            }).ToList(),
            SpecificCategoryAnalysis = categoryId.HasValue ? await AnalyzeSpecificCategory(categoryId.Value, cancellationToken) : null
        };

        return result;
    }

    private async Task<object> AnalyzeSpecificCategory(Guid categoryId, CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        var productRepository = _unitOfWork.Repository<Product>();

        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
        {
            return new { Error = "Category not found" };
        }

        var categoryIds = await GetCategoryIdsIncludingSubcategories(categoryId, cancellationToken);
        var productsInCategories = await productRepository.FindAsync(p => categoryIds.Contains(p.CategoryId), cancellationToken);

        return new
        {
            Category = new { category.Id, category.Name, category.ParentCategoryId },
            AllCategoryIds = categoryIds,
            TotalProductsInHierarchy = productsInCategories.Count(),
            ActiveProductsInHierarchy = productsInCategories.Count(p => p.IsActive),
            ProductsByCategory = categoryIds.Select(cId => new
            {
                CategoryId = cId,
                CategoryName = categoryRepository.GetByIdAsync(cId, cancellationToken).Result?.Name ?? "Unknown",
                ProductCount = productsInCategories.Count(p => p.CategoryId == cId),
                ActiveProductCount = productsInCategories.Count(p => p.CategoryId == cId && p.IsActive),
                Products = productsInCategories.Where(p => p.CategoryId == cId).Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.IsActive,
                    p.CategoryId
                }).ToList()
            }).ToList()
        };
    }

    private async Task<List<Guid>> GetCategoryIdsIncludingSubcategories(Guid categoryId, CancellationToken cancellationToken)
    {
        var categoryIds = new List<Guid> { categoryId };
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        // Debug: Check if the category exists
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        System.Console.WriteLine($"DEBUG: Category {categoryId} exists: {category != null}");
        if (category != null)
        {
            System.Console.WriteLine($"DEBUG: Category name: {category.Name}, ParentId: {category.ParentCategoryId}");
            System.Console.WriteLine($"DEBUG: Is parent category: {!category.ParentCategoryId.HasValue}");
        }
        else
        {
            System.Console.WriteLine($"DEBUG: Category {categoryId} NOT FOUND in database!");
            return categoryIds; // Return just the original ID even if not found
        }
        
        // Get all subcategories recursively
        await GetSubcategoryIdsRecursive(categoryId, categoryIds, categoryRepository, cancellationToken);
        
        System.Console.WriteLine($"DEBUG: Final category IDs list: {string.Join(", ", categoryIds)}");
        return categoryIds;
    }

    private async Task GetSubcategoryIdsRecursive(Guid parentCategoryId, List<Guid> categoryIds, IRepository<Category> categoryRepository, CancellationToken cancellationToken)
    {
        var subcategories = await categoryRepository.FindAsync(c => c.ParentCategoryId == parentCategoryId, cancellationToken);
        
        System.Console.WriteLine($"DEBUG: Found {subcategories.Count()} subcategories for parent {parentCategoryId}");
        
        foreach (var subcategory in subcategories)
        {
            System.Console.WriteLine($"DEBUG: Processing subcategory {subcategory.Id} ({subcategory.Name})");
            if (!categoryIds.Contains(subcategory.Id))
            {
                categoryIds.Add(subcategory.Id);
                // Recursively get subcategories of this subcategory
                await GetSubcategoryIdsRecursive(subcategory.Id, categoryIds, categoryRepository, cancellationToken);
            }
        }
    }

    private async Task ApplyFavoriteStatusToProductList(IEnumerable<ProductListDto> products, Guid userId, CancellationToken cancellationToken)
    {
        var productIds = products.Select(p => p.Id).ToList();
        var userFavorites = await _unitOfWork.Repository<UserFavorite>()
            .FindAsync(f => f.UserId == userId && productIds.Contains(f.ProductId), cancellationToken);
        
        var favoriteProductIds = userFavorites.Select(f => f.ProductId).ToHashSet();
        
        foreach (var product in products)
        {
            product.IsFavorite = favoriteProductIds.Contains(product.Id);
        }
    }

    private async Task ApplyFavoriteStatusToProduct(ProductDto product, Guid userId, CancellationToken cancellationToken)
    {
        var isFavorite = await _unitOfWork.Repository<UserFavorite>()
            .AnyAsync(f => f.UserId == userId && f.ProductId == product.Id, cancellationToken);
        
        product.IsFavorite = isFavorite;
    }

    private async Task ApplyFiltersToProductList(IEnumerable<ProductListDto> products, CancellationToken cancellationToken)
    {
        var productIds = products.Select(p => p.Id).ToList();
        
        if (!productIds.Any())
            return;

        var productAttributeValues = await _unitOfWork.Repository<ProductAttributeValue>()
            .FindAsync(pav => productIds.Contains(pav.ProductId), cancellationToken);

        var filterIds = productAttributeValues.Select(pav => pav.FilterId).Distinct().ToList();
        var filterOptionIds = productAttributeValues.Where(pav => pav.FilterOptionId.HasValue)
            .Select(pav => pav.FilterOptionId!.Value).Distinct().ToList();

        var filters = new Dictionary<Guid, Filter>();
        var filterOptions = new Dictionary<Guid, FilterOption>();

        if (filterIds.Any())
        {
            var filtersFromDb = await _unitOfWork.Repository<Filter>()
                .FindAsync(f => filterIds.Contains(f.Id), cancellationToken);
            filters = filtersFromDb.ToDictionary(f => f.Id, f => f);
        }

        if (filterOptionIds.Any())
        {
            var filterOptionsFromDb = await _unitOfWork.Repository<FilterOption>()
                .FindAsync(fo => filterOptionIds.Contains(fo.Id), cancellationToken);
            filterOptions = filterOptionsFromDb.ToDictionary(fo => fo.Id, fo => fo);
        }

        var productFiltersMap = productAttributeValues
            .GroupBy(pav => pav.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var product in products)
        {
            if (productFiltersMap.TryGetValue(product.Id, out var productFilters))
            {
                product.Filters = productFilters.Select(pf =>
                {
                    var filter = filters.GetValueOrDefault(pf.FilterId);
                    var filterOption = pf.FilterOptionId.HasValue ? filterOptions.GetValueOrDefault(pf.FilterOptionId.Value) : null;

                    return new ProductFilterDto
                    {
                        FilterId = pf.FilterId,
                        FilterName = filter?.Name ?? "Unknown Filter",
                        FilterType = filter?.Type ?? FilterType.Text,
                        FilterOptionId = pf.FilterOptionId,
                        FilterOptionValue = filterOption?.Value,
                        FilterOptionDisplayName = filterOption?.DisplayName,
                        CustomValue = pf.CustomValue,
                        Color = filterOption?.Color,
                        IconUrl = filterOption?.IconUrl
                    };
                }).ToList();
            }
        }
    }

    public async Task CleanAllDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Delete all data in the correct order to respect foreign key constraints
            var productAttributeValues = await _unitOfWork.Repository<ProductAttributeValue>().FindAsync(x => true, cancellationToken);
            if (productAttributeValues.Any())
            {
                _unitOfWork.Repository<ProductAttributeValue>().RemoveRange(productAttributeValues);
            }

            var productImages = await _unitOfWork.Repository<ProductImage>().FindAsync(x => true, cancellationToken);
            if (productImages.Any())
            {
                _unitOfWork.Repository<ProductImage>().RemoveRange(productImages);
            }



            var productSpecifications = await _unitOfWork.Repository<ProductSpecification>().FindAsync(x => true, cancellationToken);
            if (productSpecifications.Any())
            {
                _unitOfWork.Repository<ProductSpecification>().RemoveRange(productSpecifications);
            }

            var productPdfs = await _unitOfWork.Repository<ProductPdf>().FindAsync(x => true, cancellationToken);
            if (productPdfs.Any())
            {
                _unitOfWork.Repository<ProductPdf>().RemoveRange(productPdfs);
            }

            var downloadableFiles = await _unitOfWork.Repository<DownloadableFile>().FindAsync(x => true, cancellationToken);
            if (downloadableFiles.Any())
            {
                _unitOfWork.Repository<DownloadableFile>().RemoveRange(downloadableFiles);
            }

            var userFavorites = await _unitOfWork.Repository<UserFavorite>().FindAsync(x => true, cancellationToken);
            if (userFavorites.Any())
            {
                _unitOfWork.Repository<UserFavorite>().RemoveRange(userFavorites);
            }

            var cartItems = await _unitOfWork.Repository<CartItem>().FindAsync(x => true, cancellationToken);
            if (cartItems.Any())
            {
                _unitOfWork.Repository<CartItem>().RemoveRange(cartItems);
            }

            var carts = await _unitOfWork.Repository<Cart>().FindAsync(x => true, cancellationToken);
            if (carts.Any())
            {
                _unitOfWork.Repository<Cart>().RemoveRange(carts);
            }

            var refreshTokens = await _unitOfWork.Repository<RefreshToken>().FindAsync(x => true, cancellationToken);
            if (refreshTokens.Any())
            {
                _unitOfWork.Repository<RefreshToken>().RemoveRange(refreshTokens);
            }

            var products = await _unitOfWork.Repository<Product>().FindAsync(x => true, cancellationToken);
            if (products.Any())
            {
                _unitOfWork.Repository<Product>().RemoveRange(products);
            }

            var filterOptions = await _unitOfWork.Repository<FilterOption>().FindAsync(x => true, cancellationToken);
            if (filterOptions.Any())
            {
                _unitOfWork.Repository<FilterOption>().RemoveRange(filterOptions);
            }

            var filters = await _unitOfWork.Repository<Filter>().FindAsync(x => true, cancellationToken);
            if (filters.Any())
            {
                _unitOfWork.Repository<Filter>().RemoveRange(filters);
            }

            var banners = await _unitOfWork.Repository<Banner>().FindAsync(x => true, cancellationToken);
            if (banners.Any())
            {
                _unitOfWork.Repository<Banner>().RemoveRange(banners);
            }

            var categories = await _unitOfWork.Repository<Category>().FindAsync(x => true, cancellationToken);
            if (categories.Any())
            {
                _unitOfWork.Repository<Category>().RemoveRange(categories);
            }

            // Delete ALL users (including admin users)
            var allUsers = await _unitOfWork.Repository<User>().FindAsync(x => true, cancellationToken);
            if (allUsers.Any())
            {
                _unitOfWork.Repository<User>().RemoveRange(allUsers);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to clean database: {ex.Message}", ex);
        }
    }

    public async Task AddAzerbaijaniCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if categories already exist
            var existingCategories = await _unitOfWork.Repository<Category>().FindAsync(c => c.Name.Contains("Ticarət") || c.Name.Contains("Kompüter"), cancellationToken);
            if (existingCategories.Any())
            {
                throw new InvalidOperationException("Azerbaijani categories already exist in the database.");
            }

            var categories = new List<Category>();
            var sortOrder = 1;

            // 1. Ticarət avadanlıqları (Trade Equipment)
            var ticaretId = Guid.NewGuid();
            var ticaret = new Category
            {
                Id = ticaretId,
                Name = "Ticarət avadanlıqları",
                Slug = "ticaret-avadanliqlari",
                Description = "Ticarət üçün lazım olan avadanlıqlar",
                IsActive = true,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow
            };
            categories.Add(ticaret);

            // Subcategories for Ticarət avadanlıqları
            var ticaretSubcategories = new[]
            {
                ("POS Komputerlər", "pos-komputerler", "POS sistemlər üçün komputerlər"),
                ("Çek priterlər", "cek-printerler", "Çek çap edən priterlər"),
                ("Barkod printerlər", "barkod-printerler", "Barkod çap edən priterlər"),
                ("Mini printerlər", "mini-printerler", "Kiçik ölçülü priterlər"),
                ("Barkod scanerlər", "barkod-scanerler", "Barkod oxuyan cihazlar"),
                ("Tərəzilər", "tereziler", "Çəki ölçən tərəzilər"),
                ("Pul yeşikləri", "pul-yesikleri", "Pul saxlama yeşikləri"),
                ("Çek və Barkod kağızları", "cek-ve-barkod-kagizlari", "Çek və barkod üçün kağızlar")
            };

            foreach (var (name, slug, description) in ticaretSubcategories)
            {
                categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    IsActive = true,
                    SortOrder = categories.Count(c => c.ParentCategoryId == ticaretId) + 1,
                    ParentCategoryId = ticaretId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. Kompüterlər (Computers)
            var komputerlerId = Guid.NewGuid();
            var komputerler = new Category
            {
                Id = komputerlerId,
                Name = "Kompüterlər",
                Slug = "komputerler",
                Description = "Müxtəlif növ kompüterlər",
                IsActive = true,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow
            };
            categories.Add(komputerler);

            // Subcategories for Kompüterlər
            var komputerlerSubcategories = new[]
            {
                ("Ofis Kompüterləri", "ofis-komputerleri", "Ofis işləri üçün kompüterlər"),
                ("Oyun və Dizayn Kompüterləri", "oyun-ve-dizayn-komputerleri", "Oyun və dizayn üçün güclü kompüterlər"),
                ("Monoboklar", "monoboklar", "Bir hissədə kompüterlər"),
                ("Mini Kompüterləri", "mini-komputerleri", "Kiçik ölçülü kompüterlər")
            };

            foreach (var (name, slug, description) in komputerlerSubcategories)
            {
                categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    IsActive = true,
                    SortOrder = categories.Count(c => c.ParentCategoryId == komputerlerId) + 1,
                    ParentCategoryId = komputerlerId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 3. Noutbuklar (Laptops)
            var noutbuklarId = Guid.NewGuid();
            var noutbuklar = new Category
            {
                Id = noutbuklarId,
                Name = "Noutbuklar",
                Slug = "noutbuklar",
                Description = "Müxtəlif növ noutbuklar",
                IsActive = true,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow
            };
            categories.Add(noutbuklar);

            // Subcategories for Noutbuklar
            var noutbuklarSubcategories = new[]
            {
                ("Ofis Noutbukları", "ofis-noutbuklari", "Ofis işləri üçün noutbuklar"),
                ("Oyun Noutbukları", "oyun-noutbuklari", "Oyun üçün güclü noutbuklar"),
                ("Planşet tipli", "planset-tipli", "Planşet tipli noutbuklar")
            };

            foreach (var (name, slug, description) in noutbuklarSubcategories)
            {
                categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    IsActive = true,
                    SortOrder = categories.Count(c => c.ParentCategoryId == noutbuklarId) + 1,
                    ParentCategoryId = noutbuklarId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 4. Müşahidə sistemləri (Surveillance Systems)
            var musahideId = Guid.NewGuid();
            var musahide = new Category
            {
                Id = musahideId,
                Name = "Müşahidə sistemləri",
                Slug = "musahide-sistemleri",
                Description = "Təhlükəsizlik və müşahidə sistemləri",
                IsActive = true,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow
            };
            categories.Add(musahide);

            // Subcategories for Müşahidə sistemləri
            var musahideSubcategories = new[]
            {
                ("Analoq Kamera sistemləri", "analoq-kamera-sistemleri", "Analoq kamera sistemləri"),
                ("İP Kamera sistemləri", "ip-kamera-sistemleri", "İP kamera sistemləri"),
                ("WIFI Kameraları", "wifi-kameralari", "WIFI kameralar"),
                ("Yaddaş Qurğuları", "yaddas-qurgulari", "Yaddaş qurğuları"),
                ("Damafonlar", "damafonlar", "Damafon sistemləri"),
                ("Access Control", "access-control", "Giriş nəzarət sistemləri")
            };

            foreach (var (name, slug, description) in musahideSubcategories)
            {
                categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    IsActive = true,
                    SortOrder = categories.Count(c => c.ParentCategoryId == musahideId) + 1,
                    ParentCategoryId = musahideId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 5. Kompüter avadanlıqları (Computer Equipment)
            var komputerAvadanliqlariId = Guid.NewGuid();
            var komputerAvadanliqlari = new Category
            {
                Id = komputerAvadanliqlariId,
                Name = "Kompüter avadanlıqları",
                Slug = "komputer-avadanliqlari",
                Description = "Kompüter üçün avadanlıqlar",
                IsActive = true,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow
            };
            categories.Add(komputerAvadanliqlari);

            // Subcategories for Kompüter avadanlıqları
            var komputerAvadanliqlariSubcategories = new[]
            {
                ("Monitor", "monitor", "Kompüter monitorları"),
                ("SSD", "ssd", "SSD sürücüləri"),
                ("HDD", "hdd-avadanliq", "Hard disk sürücüləri"),
                ("RAM", "ram", "RAM yaddaşları"),
                ("CPU", "cpu", "Prosessorlar"),
                ("Case", "case", "Kompüter qutuları"),
                ("Qida Bloku", "qida-bloku", "Qida blokları"),
                ("Qulaqlıq", "qulaqliq", "Qulaqlıqlar"),
                ("Klavyatura", "klavyatura", "Klavyaturalar"),
                ("Maus", "maus", "Mauslar"),
                ("Dinamik", "dinamik", "Dinamiklər")
            };

            foreach (var (name, slug, description) in komputerAvadanliqlariSubcategories)
            {
                categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    IsActive = true,
                    SortOrder = categories.Count(c => c.ParentCategoryId == komputerAvadanliqlariId) + 1,
                    ParentCategoryId = komputerAvadanliqlariId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 6. Ofis avadanlıqları (Office Equipment)
            var ofisAvadanliqlariId = Guid.NewGuid();
            var ofisAvadanliqlari = new Category
            {
                Id = ofisAvadanliqlariId,
                Name = "Ofis avadanlıqları",
                Slug = "ofis-avadanliqlari",
                Description = "Ofis üçün avadanlıqlar",
                IsActive = true,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow
            };
            categories.Add(ofisAvadanliqlari);

            // Subcategories for Ofis avadanlıqları
            var ofisAvadanliqlariSubcategories = new[]
            {
                ("UPS", "ups", "UPS sistemləri"),
                ("Printer", "printer", "Priterlər"),
                ("Uzadıcı", "uzadici", "Uzadıcılar")
            };

            foreach (var (name, slug, description) in ofisAvadanliqlariSubcategories)
            {
                categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    IsActive = true,
                    SortOrder = categories.Count(c => c.ParentCategoryId == ofisAvadanliqlariId) + 1,
                    ParentCategoryId = ofisAvadanliqlariId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 7. Şəbəkə avadanlıqları (Network Equipment)
            var sebekeAvadanliqlariId = Guid.NewGuid();
            var sebekeAvadanliqlari = new Category
            {
                Id = sebekeAvadanliqlariId,
                Name = "Şəbəkə avadanlıqları",
                Slug = "sebeke-avadanliqlari",
                Description = "Şəbəkə üçün avadanlıqlar",
                IsActive = true,
                SortOrder = sortOrder++,
                CreatedAt = DateTime.UtcNow
            };
            categories.Add(sebekeAvadanliqlari);

            // Subcategories for Şəbəkə avadanlıqları
            var sebekeAvadanliqlariSubcategories = new[]
            {
                ("Router", "router", "Routerlər"),
                ("Access point", "access-point", "Access pointlər"),
                ("Range extender", "range-extender", "Range extenderlər"),
                ("Switch", "switch", "Switchlər"),
                ("Wifi adapter", "wifi-adapter", "Wifi adapterlər")
            };

            foreach (var (name, slug, description) in sebekeAvadanliqlariSubcategories)
            {
                categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    IsActive = true,
                    SortOrder = categories.Count(c => c.ParentCategoryId == sebekeAvadanliqlariId) + 1,
                    ParentCategoryId = sebekeAvadanliqlariId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add all categories to database
            await _unitOfWork.Repository<Category>().AddRangeAsync(categories);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to add Azerbaijani categories: {ex.Message}", ex);
        }
    }

    private async Task PopulateCategoryBreadcrumbs(IEnumerable<ProductListDto> products, CancellationToken cancellationToken)
    {
        var productList = products.ToList();
        if (!productList.Any())
            return;

        // Get all unique category IDs from products
        var categoryIds = productList.Select(p => p.CategoryId).Distinct().ToList();
        
        // Get all categories with their parent information
        var categories = await _unitOfWork.Repository<Category>()
            .FindAsync(c => categoryIds.Contains(c.Id), cancellationToken);

        // Get parent categories for subcategories
        var parentCategoryIds = categories
            .Where(c => c.ParentCategoryId.HasValue)
            .Select(c => c.ParentCategoryId!.Value)
            .Distinct()
            .ToList();

        var parentCategories = new Dictionary<Guid, Category>();
        if (parentCategoryIds.Any())
        {
            var parents = await _unitOfWork.Repository<Category>()
                .FindAsync(c => parentCategoryIds.Contains(c.Id), cancellationToken);
            parentCategories = parents.ToDictionary(c => c.Id, c => c);
        }

        // Create a lookup for categories
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        // Get brand information for products that have brands
        var brandIds = productList.Where(p => p.BrandId.HasValue).Select(p => p.BrandId!.Value).Distinct().ToList();
        var brandLookup = new Dictionary<Guid, Brand>();
        if (brandIds.Any())
        {
            var brands = await _unitOfWork.Repository<Brand>()
                .FindAsync(b => brandIds.Contains(b.Id), cancellationToken);
            brandLookup = brands.ToDictionary(b => b.Id, b => b);
        }

        // Populate breadcrumb information for each product
        foreach (var product in productList)
        {
            if (categoryLookup.TryGetValue(product.CategoryId, out var category))
            {
                // Set category slug
                product.CategorySlug = category.Slug;
                
                if (category.ParentCategoryId.HasValue && parentCategories.TryGetValue(category.ParentCategoryId.Value, out var parentCategory))
                {
                    // This is a subcategory
                    product.ParentCategoryName = parentCategory.Name;
                    product.ParentCategorySlug = parentCategory.Slug;
                    product.SubCategoryName = category.Name;
                    product.SubCategorySlug = category.Slug;
                }
                else
                {
                    // This is a parent category
                    product.ParentCategoryName = category.Name;
                    product.ParentCategorySlug = category.Slug;
                    product.SubCategoryName = null;
                    product.SubCategorySlug = null;
                }
            }

            // Populate brand information
            if (product.BrandId.HasValue && brandLookup.TryGetValue(product.BrandId.Value, out var brand))
            {
                product.BrandName = brand.Name;
            }
        }
    }

    private async Task PopulateCategoryBreadcrumbsForSingleProduct(ProductDto? product, CancellationToken cancellationToken)
    {
        if (product == null)
            return;

        // Get the category information
        var category = await _unitOfWork.Repository<Category>()
            .GetByIdAsync(product.CategoryId, cancellationToken);

        if (category == null)
            return;

        // Set category slug
        product.CategorySlug = category.Slug;

        if (category.ParentCategoryId.HasValue)
        {
            // This is a subcategory - get parent category
            var parentCategory = await _unitOfWork.Repository<Category>()
                .GetByIdAsync(category.ParentCategoryId.Value, cancellationToken);

            if (parentCategory != null)
            {
                product.ParentCategoryName = parentCategory.Name;
                product.ParentCategorySlug = parentCategory.Slug;
                product.SubCategoryName = category.Name;
                product.SubCategorySlug = category.Slug;
            }
        }
        else
        {
            // This is a parent category
            product.ParentCategoryName = category.Name;
            product.ParentCategorySlug = category.Slug;
            product.SubCategoryName = null;
            product.SubCategorySlug = null;
        }

        // Get brand information if BrandId is present
        if (product.BrandId.HasValue)
        {
            var brand = await _unitOfWork.Repository<Brand>()
                .GetByIdAsync(product.BrandId.Value, cancellationToken);
            
            if (brand != null)
            {
                product.BrandName = brand.Name;
            }
        }
    }

    #region Pagination Methods

    public async Task<PagedResultDto<ProductListDto>> GetProductsPaginatedAsync(ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Brand);
        
        // Apply filters
        var filteredProducts = products.Where(p => p.IsActive);
        
        if (request.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryIdsIncludingSubcategories(request.CategoryId.Value, cancellationToken);
            filteredProducts = filteredProducts.Where(p => categoryIds.Contains(p.CategoryId));
        }
        
        if (!string.IsNullOrWhiteSpace(request.BrandSlug))
        {
            var brand = await _unitOfWork.Repository<Brand>()
                .FirstOrDefaultAsync(b => b.Slug == request.BrandSlug && b.IsActive, cancellationToken);
            if (brand != null)
            {
                filteredProducts = filteredProducts.Where(p => p.BrandId == brand.Id);
            }
        }
        
        if (request.IsHotDeal.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => p.IsHotDeal == request.IsHotDeal.Value);
        }
        
        if (request.IsActive.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => p.IsActive == request.IsActive.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filteredProducts = filteredProducts.Where(p => p.Name.ToLower().Contains(searchTerm));
        }
        
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => 
                (!request.MinPrice.HasValue || (p.DiscountedPrice ?? p.Price) >= request.MinPrice.Value) &&
                (!request.MaxPrice.HasValue || (p.DiscountedPrice ?? p.Price) <= request.MaxPrice.Value));
        }
        
        // Apply sorting
        filteredProducts = ApplySorting(filteredProducts.AsQueryable(), request.ProductSortBy, request.SortOrder);
        
        // Get total count before pagination
        var totalCount = filteredProducts.Count();
        
        // Apply pagination
        var pagedProducts = filteredProducts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Map to DTOs
        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(pagedProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return CreatePagedResult(productDtos, request.Page, request.PageSize, totalCount);
    }

    public async Task<PagedResultDto<ProductListDto>> GetProductsByCategoryPaginatedAsync(Guid categoryId, ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Brand);
        var categoryIds = await GetCategoryIdsIncludingSubcategories(categoryId, cancellationToken);
        var filteredProducts = products.Where(p => categoryIds.Contains(p.CategoryId) && p.IsActive);
        
        // Apply additional filters
        if (!string.IsNullOrWhiteSpace(request.BrandSlug))
        {
            var brand = await _unitOfWork.Repository<Brand>()
                .FirstOrDefaultAsync(b => b.Slug == request.BrandSlug && b.IsActive, cancellationToken);
            if (brand != null)
            {
                filteredProducts = filteredProducts.Where(p => p.BrandId == brand.Id);
            }
        }
        
        if (request.IsHotDeal.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => p.IsHotDeal == request.IsHotDeal.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filteredProducts = filteredProducts.Where(p => 
                p.Name.ToLower().Contains(searchTerm) || 
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm)));
        }
        
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => 
                (!request.MinPrice.HasValue || (p.DiscountedPrice ?? p.Price) >= request.MinPrice.Value) &&
                (!request.MaxPrice.HasValue || (p.DiscountedPrice ?? p.Price) <= request.MaxPrice.Value));
        }
        
        // Apply sorting
        filteredProducts = ApplySorting(filteredProducts.AsQueryable(), request.ProductSortBy, request.SortOrder);
        
        // Get total count before pagination
        var totalCount = filteredProducts.Count();
        
        // Apply pagination
        var pagedProducts = filteredProducts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Map to DTOs
        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(pagedProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return CreatePagedResult(productDtos, request.Page, request.PageSize, totalCount);
    }

    public async Task<PagedResultDto<ProductListDto>> GetProductsByCategorySlugPaginatedAsync(string categorySlug, ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            throw new ArgumentException("Category slug cannot be null or empty.", nameof(categorySlug));
        }

        // Find category by slug
        var category = await _unitOfWork.Repository<Category>()
            .FirstOrDefaultAsync(c => c.Slug == categorySlug && c.IsActive, cancellationToken);
        
        if (category == null)
        {
            throw new ArgumentException($"Category with slug '{categorySlug}' not found.");
        }

        return await GetProductsByCategoryPaginatedAsync(category.Id, request, userRole, userId, cancellationToken);
    }

    public async Task<PagedResultDto<ProductListDto>> GetProductsByBrandPaginatedAsync(string brandSlug, ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brandSlug))
        {
            throw new ArgumentException("Brand slug cannot be null or empty.", nameof(brandSlug));
        }

        // Find brand by slug
        var brand = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Slug == brandSlug && b.IsActive, cancellationToken);
        
        if (brand == null)
        {
            throw new ArgumentException($"Brand with slug '{brandSlug}' not found.");
        }

        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Brand);
        var filteredProducts = products.Where(p => p.BrandId == brand.Id && p.IsActive);
        
        // Apply additional filters
        if (request.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryIdsIncludingSubcategories(request.CategoryId.Value, cancellationToken);
            filteredProducts = filteredProducts.Where(p => categoryIds.Contains(p.CategoryId));
        }
        
        if (request.IsHotDeal.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => p.IsHotDeal == request.IsHotDeal.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filteredProducts = filteredProducts.Where(p => 
                p.Name.ToLower().Contains(searchTerm) || 
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm)));
        }
        
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => 
                (!request.MinPrice.HasValue || (p.DiscountedPrice ?? p.Price) >= request.MinPrice.Value) &&
                (!request.MaxPrice.HasValue || (p.DiscountedPrice ?? p.Price) <= request.MaxPrice.Value));
        }
        
        // Apply sorting
        filteredProducts = ApplySorting(filteredProducts.AsQueryable(), request.ProductSortBy, request.SortOrder);
        
        // Get total count before pagination
        var totalCount = filteredProducts.Count();
        
        // Apply pagination
        var pagedProducts = filteredProducts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Map to DTOs
        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(pagedProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return CreatePagedResult(productDtos, request.Page, request.PageSize, totalCount);
    }

    public async Task<PagedResultDto<ProductListDto>> GetHotDealsPaginatedAsync(HotDealsPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Brand);
        var filteredProducts = products.Where(p => p.IsHotDeal && p.IsActive);
        
        if (!string.IsNullOrWhiteSpace(request.BrandSlug))
        {
            var brand = await _unitOfWork.Repository<Brand>()
                .FirstOrDefaultAsync(b => b.Slug == request.BrandSlug && b.IsActive, cancellationToken);
            if (brand != null)
            {
                filteredProducts = filteredProducts.Where(p => p.BrandId == brand.Id);
            }
        }
        
        if (request.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryIdsIncludingSubcategories(request.CategoryId.Value, cancellationToken);
            filteredProducts = filteredProducts.Where(p => categoryIds.Contains(p.CategoryId));
        }
        
        filteredProducts = ApplySorting(filteredProducts.AsQueryable(), request.ProductSortBy, request.SortOrder);
        
        // Get total count before pagination
        var totalCount = filteredProducts.Count();
        
        // Apply pagination
        var pagedProducts = filteredProducts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Map to DTOs
        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(pagedProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return CreatePagedResult(productDtos, request.Page, request.PageSize, totalCount);
    }

    public async Task<PagedResultDto<ProductListDto>> SearchProductsPaginatedAsync(SearchPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            throw new ArgumentException("Search term is required.", nameof(request.SearchTerm));
        }

        var products = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Brand);
        var searchTerm = request.SearchTerm.ToLower();
        
        var filteredProducts = products.Where(p => p.IsActive && p.Name.ToLower().Contains(searchTerm));
        
        // Apply additional filters
        if (request.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryIdsIncludingSubcategories(request.CategoryId.Value, cancellationToken);
            filteredProducts = filteredProducts.Where(p => categoryIds.Contains(p.CategoryId));
        }
        
        if (!string.IsNullOrWhiteSpace(request.BrandSlug))
        {
            var brand = await _unitOfWork.Repository<Brand>()
                .FirstOrDefaultAsync(b => b.Slug == request.BrandSlug && b.IsActive, cancellationToken);
            if (brand != null)
            {
                filteredProducts = filteredProducts.Where(p => p.BrandId == brand.Id);
            }
        }
        
        if (request.IsHotDeal.HasValue)
        {
            filteredProducts = filteredProducts.Where(p => p.IsHotDeal == request.IsHotDeal.Value);
        }
        
        // Apply sorting
        filteredProducts = ApplySorting(filteredProducts.AsQueryable(), request.ProductSortBy, request.SortOrder);
        
        // Get total count before pagination
        var totalCount = filteredProducts.Count();
        
        // Apply pagination
        var pagedProducts = filteredProducts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Map to DTOs
        var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(pagedProducts);
        
        if (userId.HasValue)
        {
            await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
        }
        
        await ApplyFiltersToProductList(productDtos, cancellationToken);
        await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
        
        return CreatePagedResult(productDtos, request.Page, request.PageSize, totalCount);
    }

    public async Task<PagedResultDto<ProductListDto>> GetRecommendedProductsPaginatedAsync(RecommendedProductsPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        // When categoryId is provided, get products directly from that category for proper pagination
        if (request.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryIdsIncludingSubcategories(request.CategoryId.Value, cancellationToken);
            
            // Get all active products from the category with includes
            var allProducts = await _unitOfWork.Repository<Product>().GetAllWithIncludesAsync(p => p.Category, p => p.Images);
            var categoryProducts = allProducts
                .Where(p => p.IsActive && categoryIds.Contains(p.CategoryId))
                .ToList();
            
            // Apply brand filter if specified
            if (!string.IsNullOrWhiteSpace(request.BrandSlug))
            {
                var brand = await _unitOfWork.Repository<Brand>()
                    .FirstOrDefaultAsync(b => b.Slug == request.BrandSlug && b.IsActive, cancellationToken);
                if (brand != null)
                {
                    categoryProducts = categoryProducts.Where(p => p.BrandId == brand.Id).ToList();
                }
            }
            
            // Map to DTOs
            var productDtos = _mapper.Map<IEnumerable<ProductListDto>>(categoryProducts);
            
            // Apply favorite status if user is authenticated
            if (userId.HasValue)
            {
                await ApplyFavoriteStatusToProductList(productDtos, userId.Value, cancellationToken);
            }
            
            // Apply filters and breadcrumbs
            await ApplyFiltersToProductList(productDtos, cancellationToken);
            await PopulateCategoryBreadcrumbs(productDtos, cancellationToken);
            
            // Apply sorting
            var sortedProducts = ApplyInMemorySorting(productDtos, request.ProductSortBy, request.SortOrder);
            
            var totalCount = sortedProducts.Count();
            var pagedProducts = sortedProducts
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
            
            return CreatePagedResult(pagedProducts, request.Page, request.PageSize, totalCount);
        }
        
        // When categoryId is not provided, use recommendation logic
        var recommendationRequest = new RecommendationRequestDto
        {
            ProductId = request.ProductId,
            CategoryId = null,
            Limit = request.Limit
        };
        
        var recommendations = await GetRecommendedProductsAsync(recommendationRequest, userRole, userId, cancellationToken);
        
        var allProductsList = new List<ProductListDto>();
        allProductsList.AddRange(recommendations.BasedOnFavorites);
        allProductsList.AddRange(recommendations.BasedOnCategory);
        allProductsList.AddRange(recommendations.HotDeals);
        allProductsList.AddRange(recommendations.RecentlyAdded);
        allProductsList.AddRange(recommendations.SimilarProducts);
        
        var uniqueProducts = allProductsList
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();
        
        IEnumerable<ProductListDto> filteredProducts = uniqueProducts;
        
        if (!string.IsNullOrWhiteSpace(request.BrandSlug))
        {
            var brand = await _unitOfWork.Repository<Brand>()
                .FirstOrDefaultAsync(b => b.Slug == request.BrandSlug && b.IsActive, cancellationToken);
            if (brand != null)
            {
                filteredProducts = filteredProducts.Where(p => p.BrandId == brand.Id);
            }
        }
        
        var sortedProductsList = ApplyInMemorySorting(filteredProducts.ToList(), request.ProductSortBy, request.SortOrder);
        
        var totalCountList = sortedProductsList.Count();
        
        var pagedProductsList = sortedProductsList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        return CreatePagedResult(pagedProductsList, request.Page, request.PageSize, totalCountList);
    }

    #endregion

    #region Helper Methods for Pagination


    private IQueryable<Product> ApplySorting(IQueryable<Product> products, ProductSortOption sortBy, string sortOrder)
    {
        return sortBy switch
        {
            ProductSortOption.Name => sortOrder.ToLower() == "desc" 
                ? products.OrderByDescending(p => p.Name)
                : products.OrderBy(p => p.Name),
            ProductSortOption.Price => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.DiscountedPrice ?? p.Price)
                : products.OrderBy(p => p.DiscountedPrice ?? p.Price),
            ProductSortOption.CreatedAt => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.CreatedAt)
                : products.OrderBy(p => p.CreatedAt),
            ProductSortOption.StockQuantity => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.StockQuantity)
                : products.OrderBy(p => p.StockQuantity),
            ProductSortOption.CategoryName => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.Category.Name)
                : products.OrderBy(p => p.Category.Name),
            ProductSortOption.BrandName => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.Brand != null ? p.Brand.Name : "")
                : products.OrderBy(p => p.Brand != null ? p.Brand.Name : ""),
            _ => products.OrderBy(p => p.Name)
        };
    }

    private IQueryable<ProductListDto> ApplySorting(IQueryable<ProductListDto> products, ProductSortOption sortBy, string sortOrder)
    {
        return sortBy switch
        {
            ProductSortOption.Name => sortOrder.ToLower() == "desc" 
                ? products.OrderByDescending(p => p.Name)
                : products.OrderBy(p => p.Name),
            ProductSortOption.Price => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.DiscountedPrice ?? p.Price)
                : products.OrderBy(p => p.DiscountedPrice ?? p.Price),
            ProductSortOption.CreatedAt => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.Name) // ProductListDto doesn't have CreatedAt, using Name as fallback
                : products.OrderBy(p => p.Name),
            ProductSortOption.StockQuantity => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.StockQuantity)
                : products.OrderBy(p => p.StockQuantity),
            ProductSortOption.CategoryName => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.CategoryName)
                : products.OrderBy(p => p.CategoryName),
            ProductSortOption.BrandName => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.BrandName ?? "")
                : products.OrderBy(p => p.BrandName ?? ""),
            _ => products.OrderBy(p => p.Name)
        };
    }

    private IEnumerable<ProductListDto> ApplyInMemorySorting(IEnumerable<ProductListDto> products, ProductSortOption sortBy, string sortOrder)
    {
        return sortBy switch
        {
            ProductSortOption.Name => sortOrder.ToLower() == "desc" 
                ? products.OrderByDescending(p => p.Name)
                : products.OrderBy(p => p.Name),
            ProductSortOption.Price => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.DiscountedPrice ?? p.Price)
                : products.OrderBy(p => p.DiscountedPrice ?? p.Price),
            ProductSortOption.CreatedAt => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.Name) // ProductListDto doesn't have CreatedAt, using Name as fallback
                : products.OrderBy(p => p.Name),
            ProductSortOption.StockQuantity => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.StockQuantity)
                : products.OrderBy(p => p.StockQuantity),
            ProductSortOption.CategoryName => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.CategoryName)
                : products.OrderBy(p => p.CategoryName),
            ProductSortOption.BrandName => sortOrder.ToLower() == "desc"
                ? products.OrderByDescending(p => p.BrandName ?? "")
                : products.OrderBy(p => p.BrandName ?? ""),
            _ => products.OrderBy(p => p.Name)
        };
    }

    private PagedResultDto<ProductListDto> CreatePagedResult(IEnumerable<ProductListDto> items, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        return new PagedResultDto<ProductListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    #endregion
}
