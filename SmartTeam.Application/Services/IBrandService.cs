using SmartTeam.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace SmartTeam.Application.Services;

public interface IBrandService
{
    Task<IEnumerable<BrandDto>> GetAllBrandsAsync(CancellationToken cancellationToken = default);
    Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandDto?> GetBrandBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateBrandWithImageAsync(CreateBrandWithImageDto createBrandDto, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateBrandWithImageAsync(Guid id, UpdateBrandWithImageDto updateBrandDto, IFormFile? imageFile, CancellationToken cancellationToken = default);
    Task<bool> DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AddBrandsResultDto> AddPredefinedBrandsAsync(CancellationToken cancellationToken = default);
    
    // Pagination methods
    Task<PagedResultDto<BrandDto>> GetBrandsPaginatedAsync(BrandPaginationRequestDto request, CancellationToken cancellationToken = default);
}
