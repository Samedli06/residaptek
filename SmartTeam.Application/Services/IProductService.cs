using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace SmartTeam.Application.Services;

public interface IProductService
{
    Task<IEnumerable<ProductListDto>> GetAllProductsAsync(UserRole? userRole = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListDto>> GetAllProductsAsync(UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListDto>> GetProductsByCategoryAsync(Guid categoryId, UserRole? userRole = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListDto>> GetProductsByCategoryAsync(Guid categoryId, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListDto>> GetProductsByCategorySlugAsync(string categorySlug, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListDto>> GetHotDealsAsync(UserRole? userRole = null, int? limit = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductListDto>> GetProductsByBrandAsync(string brandSlug, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, UserRole? userRole = null, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductBySlugAsync(string slug, UserRole? userRole = null, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductWithImageAsync(CreateProductWithImageDto createProductDto, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductWithImageAsync(Guid id, UpdateProductDto updateProductDto, IFormFile? imageFile, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductWithFilesAsync(Guid id, UpdateProductDto updateProductDto, IFormFile? imageFile, IFormFile[]? detailImageFiles, IFormFile? pdfFile, Guid? userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductListDto>> SearchProductsAsync(string searchTerm, UserRole? userRole = null, CancellationToken cancellationToken = default);
    Task<GlobalSearchResultDto> GlobalSearchAsync(string searchTerm, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ProductDto> UploadProductImageAsync(Guid productId, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<ProductDto> UploadProductImagesAsync(Guid productId, IFormFileCollection imageFiles, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductImageAsync(Guid productId, string imageUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductDetailImageAsync(Guid productId, string imageUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductDetailImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductStockDto>> GetProductStockStatusAsync(CancellationToken cancellationToken = default);
    Task<StockSummaryDto> GetStockSummaryAsync(CancellationToken cancellationToken = default);
    Task<RecommendedProductsDto> GetRecommendedProductsAsync(RecommendationRequestDto? request = null, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ProductSpecificationDto?> GetProductSpecificationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductSpecificationDto> CreateProductSpecificationsAsync(CreateProductSpecificationDto createDto, CancellationToken cancellationToken = default);
    Task<ProductSpecificationDto> UpdateProductSpecificationsAsync(Guid productId, UpdateProductSpecificationDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductSpecificationsAsync(Guid productId, CancellationToken cancellationToken = default);
    
    // Product filtering methods
    Task<FilteredProductsResultDto> GetFilteredProductsAsync(ProductFilterCriteriaDto criteria, UserRole? userRole = null, CancellationToken cancellationToken = default);
    
    // Pagination methods
    Task<PagedResultDto<ProductListDto>> GetProductsPaginatedAsync(ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProductListDto>> GetProductsByCategoryPaginatedAsync(Guid categoryId, ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProductListDto>> GetProductsByCategorySlugPaginatedAsync(string categorySlug, ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProductListDto>> GetProductsByBrandPaginatedAsync(string brandSlug, ProductPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProductListDto>> GetHotDealsPaginatedAsync(HotDealsPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProductListDto>> SearchProductsPaginatedAsync(SearchPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<ProductListDto>> GetRecommendedProductsPaginatedAsync(RecommendedProductsPaginationRequestDto request, UserRole? userRole = null, Guid? userId = null, CancellationToken cancellationToken = default);
    
    // Diagnostic methods
    Task<object> DiagnoseCategoryStructureAsync(Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<object> TestParentCategoryFilteringAsync(Guid parentCategoryId, CancellationToken cancellationToken = default);
    
    // Database management methods
    Task CleanAllDataAsync(CancellationToken cancellationToken = default);
    Task AddAzerbaijaniCategoriesAsync(CancellationToken cancellationToken = default);
}
