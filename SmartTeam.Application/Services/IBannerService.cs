using SmartTeam.Application.DTOs;
using Microsoft.AspNetCore.Http;
using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.Services;

public interface IBannerService
{
    // Banner management
    Task<IEnumerable<BannerDto>> GetAllBannersAsync(CancellationToken cancellationToken = default);
    Task<PagedBannerResultDto> SearchBannersAsync(BannerSearchDto searchDto, CancellationToken cancellationToken = default);
    Task<BannerDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BannerDto> CreateBannerAsync(CreateBannerDto createBannerDto, CancellationToken cancellationToken = default);
    Task<BannerDto> CreateBannerWithImageAsync(CreateBannerWithImageDto createBannerDto, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<BannerDto> CreateBannerWithImagesAsync(CreateBannerWithImageDto createBannerDto, IFormFile imageFile, IFormFile? mobileImageFile = null, CancellationToken cancellationToken = default);
    Task<BannerDto> UpdateBannerAsync(Guid id, UpdateBannerDto updateBannerDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteBannerAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Image management
    Task<BannerDto> UploadBannerImageAsync(Guid bannerId, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<BannerDto> UploadBannerMobileImageAsync(Guid bannerId, IFormFile imageFile, CancellationToken cancellationToken = default);
    Task<bool> DeleteBannerImageAsync(Guid bannerId, string imageType = "main", CancellationToken cancellationToken = default);
    
    // Utility methods
    Task<BannerStatisticsDto> GetBannerStatisticsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BannerTypeDto>> GetBannerTypesAsync();
    Task<bool> ReorderBannersAsync(List<Guid> bannerIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<BannerDto>> GetActiveBannersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BannerDto>> GetBannersByTypeAsync(BannerType type, CancellationToken cancellationToken = default);
    Task<bool> ToggleBannerStatusAsync(Guid id, CancellationToken cancellationToken = default);
}
