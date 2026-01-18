using AutoMapper;
using Microsoft.AspNetCore.Http;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class BannerService : IBannerService
{
    private readonly IRepository<Banner> _bannerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileUploadService _fileUploadService;

    public BannerService(
        IRepository<Banner> bannerRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileUploadService fileUploadService)
    {
        _bannerRepository = bannerRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileUploadService = fileUploadService;
    }

    public async Task<IEnumerable<BannerDto>> GetAllBannersAsync(CancellationToken cancellationToken = default)
    {
        var banners = await _bannerRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<BannerDto>>(banners.OrderBy(b => b.SortOrder));
    }

    public async Task<PagedBannerResultDto> SearchBannersAsync(BannerSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        var query = (await _bannerRepository.GetAllAsync(cancellationToken)).AsQueryable();

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(searchTerm));
        }

        if (searchDto.Type.HasValue)
        {
            query = query.Where(b => b.Type == searchDto.Type.Value);
        }


        if (searchDto.IsActive.HasValue)
        {
            query = query.Where(b => b.IsActive == searchDto.IsActive.Value);
        }

        if (searchDto.IsCurrentlyActive.HasValue && searchDto.IsCurrentlyActive.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(b => b.IsActive && 
                               (b.StartDate == null || b.StartDate <= now) && 
                               (b.EndDate == null || b.EndDate >= now));
        }

        if (searchDto.StartDateFrom.HasValue)
        {
            query = query.Where(b => b.StartDate >= searchDto.StartDateFrom.Value);
        }

        if (searchDto.StartDateTo.HasValue)
        {
            query = query.Where(b => b.StartDate <= searchDto.StartDateTo.Value);
        }

        // Apply sorting
        query = searchDto.SortBy?.ToLower() switch
        {
            "title" => searchDto.SortOrder == "desc" ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            "type" => searchDto.SortOrder == "desc" ? query.OrderByDescending(b => b.Type) : query.OrderBy(b => b.Type),
            "createdat" => searchDto.SortOrder == "desc" ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt),
            "startdate" => searchDto.SortOrder == "desc" ? query.OrderByDescending(b => b.StartDate) : query.OrderBy(b => b.StartDate),
            _ => searchDto.SortOrder == "desc" ? query.OrderByDescending(b => b.SortOrder) : query.OrderBy(b => b.SortOrder)
        };

        var totalCount = query.Count();
        var banners = query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();

        return new PagedBannerResultDto
        {
            Banners = _mapper.Map<IEnumerable<BannerDto>>(banners),
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize),
            HasNextPage = searchDto.Page * searchDto.PageSize < totalCount,
            HasPreviousPage = searchDto.Page > 1
        };
    }

    public async Task<BannerDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepository.GetByIdAsync(id, cancellationToken);
        return banner != null ? _mapper.Map<BannerDto>(banner) : null;
    }

    public async Task<BannerDto> CreateBannerAsync(CreateBannerDto createBannerDto, CancellationToken cancellationToken = default)
    {
        var banner = _mapper.Map<Banner>(createBannerDto);
        banner.Id = Guid.NewGuid();
        banner.CreatedAt = DateTime.UtcNow;

        await _bannerRepository.AddAsync(banner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BannerDto>(banner);
    }

    public async Task<BannerDto> CreateBannerWithImageAsync(CreateBannerWithImageDto createBannerDto, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        // Validate image file
        if (!_fileUploadService.IsValidImageFile(imageFile))
        {
            throw new ArgumentException("Invalid image file format. Please upload a valid image file.");
        }

        // Upload image
        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "banners");

        // Create banner
        var banner = _mapper.Map<Banner>(createBannerDto);
        banner.Id = Guid.NewGuid();
        banner.ImageUrl = imageUrl;
        banner.CreatedAt = DateTime.UtcNow;

        await _bannerRepository.AddAsync(banner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BannerDto>(banner);
    }

    public async Task<BannerDto> CreateBannerWithImagesAsync(CreateBannerWithImageDto createBannerDto, IFormFile imageFile, IFormFile? mobileImageFile = null, CancellationToken cancellationToken = default)
    {
        // Validate main image file
        if (!_fileUploadService.IsValidImageFile(imageFile))
        {
            throw new ArgumentException("Invalid image file format. Please upload a valid image file.");
        }

        // Validate mobile image file if provided
        if (mobileImageFile != null && !_fileUploadService.IsValidImageFile(mobileImageFile))
        {
            throw new ArgumentException("Invalid mobile image file format. Please upload a valid image file.");
        }

        // Upload main image
        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "banners");
        
        // Upload mobile image if provided
        string? mobileImageUrl = null;
        if (mobileImageFile != null)
        {
            mobileImageUrl = await _fileUploadService.UploadFileAsync(mobileImageFile, "banners");
        }

        // Create banner
        var banner = _mapper.Map<Banner>(createBannerDto);
        banner.Id = Guid.NewGuid();
        banner.ImageUrl = imageUrl;
        banner.MobileImageUrl = mobileImageUrl;
        banner.CreatedAt = DateTime.UtcNow;

        await _bannerRepository.AddAsync(banner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BannerDto>(banner);
    }

    public async Task<BannerDto> UpdateBannerAsync(Guid id, UpdateBannerDto updateBannerDto, CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepository.GetByIdAsync(id, cancellationToken);
        if (banner == null)
        {
            throw new ArgumentException($"Banner with ID {id} not found.");
        }

        _mapper.Map(updateBannerDto, banner);
        banner.UpdatedAt = DateTime.UtcNow;

        _bannerRepository.Update(banner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BannerDto>(banner);
    }

    public async Task<bool> DeleteBannerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepository.GetByIdAsync(id, cancellationToken);
        if (banner == null)
        {
            return false;
        }

        // Delete associated images
        if (!string.IsNullOrEmpty(banner.ImageUrl))
        {
            await _fileUploadService.DeleteFileAsync(banner.ImageUrl);
        }
        if (!string.IsNullOrEmpty(banner.MobileImageUrl))
        {
            await _fileUploadService.DeleteFileAsync(banner.MobileImageUrl);
        }

        _bannerRepository.Remove(banner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<BannerDto> UploadBannerImageAsync(Guid bannerId, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, cancellationToken);
        if (banner == null)
        {
            throw new ArgumentException($"Banner with ID {bannerId} not found.");
        }

        if (!_fileUploadService.IsValidImageFile(imageFile))
        {
            throw new ArgumentException("Invalid image file format. Please upload a valid image file.");
        }

        // IMPORTANT: Do not delete old images to preserve them after publish
        // Delete old image if exists
        // if (!string.IsNullOrEmpty(banner.ImageUrl))
        // {
        //     await _fileUploadService.DeleteFileAsync(banner.ImageUrl);
        // }

        // Upload new image
        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "banners");
        banner.ImageUrl = imageUrl;
        banner.UpdatedAt = DateTime.UtcNow;

        _bannerRepository.Update(banner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BannerDto>(banner);
    }

    public async Task<BannerDto> UploadBannerMobileImageAsync(Guid bannerId, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, cancellationToken);
        if (banner == null)
        {
            throw new ArgumentException($"Banner with ID {bannerId} not found.");
        }

        if (!_fileUploadService.IsValidImageFile(imageFile))
        {
            throw new ArgumentException("Invalid image file format. Please upload a valid image file.");
        }

        // IMPORTANT: Do not delete old images to preserve them after publish
        // Delete old mobile image if exists
        // if (!string.IsNullOrEmpty(banner.MobileImageUrl))
        // {
        //     await _fileUploadService.DeleteFileAsync(banner.MobileImageUrl);
        // }

        // Upload new mobile image
        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "banners");
        banner.MobileImageUrl = imageUrl;
        banner.UpdatedAt = DateTime.UtcNow;

        _bannerRepository.Update(banner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BannerDto>(banner);
    }


    public async Task<bool> DeleteBannerImageAsync(Guid bannerId, string imageType = "main", CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, cancellationToken);
        if (banner == null)
        {
            return false;
        }

        string? imageUrl = imageType.ToLower() switch
        {
            "main" => banner.ImageUrl,
            "mobile" => banner.MobileImageUrl,
            _ => null
        };

        if (string.IsNullOrEmpty(imageUrl))
        {
            return false;
        }

        // Delete image file
        await _fileUploadService.DeleteFileAsync(imageUrl);

        // Update banner
        switch (imageType.ToLower())
        {
            case "main":
                banner.ImageUrl = string.Empty;
                break;
            case "mobile":
                banner.MobileImageUrl = null;
                break;
        }

        banner.UpdatedAt = DateTime.UtcNow;
        _bannerRepository.Update(banner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<BannerStatisticsDto> GetBannerStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var banners = await _bannerRepository.GetAllAsync(cancellationToken);
        var bannersList = banners.ToList();
        var now = DateTime.UtcNow;

        return new BannerStatisticsDto
        {
            TotalBanners = bannersList.Count,
            ActiveBanners = bannersList.Count(b => b.IsActive),
            InactiveBanners = bannersList.Count(b => !b.IsActive),
            CurrentlyActiveBanners = bannersList.Count(b => b.IsActive && 
                (b.StartDate == null || b.StartDate <= now) && 
                (b.EndDate == null || b.EndDate >= now)),
            ScheduledBanners = bannersList.Count(b => b.StartDate > now),
            ExpiredBanners = bannersList.Count(b => b.EndDate < now)
        };
    }

    public async Task<IEnumerable<BannerTypeDto>> GetBannerTypesAsync()
    {
        var bannerTypes = Enum.GetValues<BannerType>().Select(type => new BannerTypeDto
        {
            Value = type,
            Name = type.ToString(),
            Description = type switch
            {
                BannerType.Hero => "Main hero banner displayed prominently",
                _ => type.ToString()
            }
        });

        return await Task.FromResult(bannerTypes);
    }


    public async Task<bool> ReorderBannersAsync(List<Guid> bannerIds, CancellationToken cancellationToken = default)
    {
        var banners = await _bannerRepository.GetAllAsync(cancellationToken);
        var bannersList = banners.ToList();

        for (int i = 0; i < bannerIds.Count; i++)
        {
            var banner = bannersList.FirstOrDefault(b => b.Id == bannerIds[i]);
            if (banner != null)
            {
                banner.SortOrder = i;
                banner.UpdatedAt = DateTime.UtcNow;
            }
        }

        _bannerRepository.UpdateRange(bannersList.Where(b => bannerIds.Contains(b.Id)));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<BannerDto>> GetActiveBannersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var banners = await _bannerRepository.FindAsync(b => b.IsActive && 
            (b.StartDate == null || b.StartDate <= now) && 
            (b.EndDate == null || b.EndDate >= now), cancellationToken);

        return _mapper.Map<IEnumerable<BannerDto>>(banners.OrderBy(b => b.SortOrder));
    }

    public async Task<IEnumerable<BannerDto>> GetBannersByTypeAsync(BannerType type, CancellationToken cancellationToken = default)
    {
        var banners = await _bannerRepository.FindAsync(b => b.Type == type && b.IsActive, cancellationToken);
        return _mapper.Map<IEnumerable<BannerDto>>(banners.OrderBy(b => b.SortOrder));
    }


    public async Task<bool> ToggleBannerStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var banner = await _bannerRepository.GetByIdAsync(id, cancellationToken);
        if (banner == null)
        {
            return false;
        }

        banner.IsActive = !banner.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;

        _bannerRepository.Update(banner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
