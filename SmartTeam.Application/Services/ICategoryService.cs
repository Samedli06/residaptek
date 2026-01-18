using SmartTeam.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace SmartTeam.Application.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryWithImageAsync(CreateCategoryWithImageDto createCategoryDto, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto updateCategoryDto, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryWithImageAsync(Guid id, UpdateCategoryWithImageDto updateCategoryDto, IFormFile? imageFile, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<CategoryDto> UploadCategoryImageAsync(Guid categoryId, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryImageAsync(Guid categoryId, string imageUrl, CancellationToken cancellationToken = default);
}
