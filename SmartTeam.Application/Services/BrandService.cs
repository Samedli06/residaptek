using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileUploadService _fileUploadService;

    public BrandService(IUnitOfWork unitOfWork, IMapper mapper, IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileUploadService = fileUploadService;
    }

    public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken);
        var activeBrands = brands.Where(b => b.IsActive).OrderBy(b => b.SortOrder);
        
        // Get all products to calculate counts
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var activeProducts = products.Where(p => p.IsActive);
        
        var brandDtos = _mapper.Map<IEnumerable<BrandDto>>(activeBrands);
        
        // Calculate product count for each brand
        foreach (var brandDto in brandDtos)
        {
            brandDto.ProductCount = activeProducts.Count(p => p.BrandId == brandDto.Id);
        }
        
        return brandDtos;
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);
        if (brand == null || !brand.IsActive)
            return null;

        var brandDto = _mapper.Map<BrandDto>(brand);
        
        // Calculate product count
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == id && p.IsActive);
        
        return brandDto;
    }

    public async Task<BrandDto?> GetBrandBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>()
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive, cancellationToken);
        
        if (brand == null)
            return null;

        var brandDto = _mapper.Map<BrandDto>(brand);
        
        // Calculate product count
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == brand.Id && p.IsActive);
        
        return brandDto;
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, CancellationToken cancellationToken = default)
    {
        var brand = new Brand();
        
        // Explicitly set all fields
        brand.Id = Guid.NewGuid();
        brand.Name = createBrandDto.Name.Trim();
        brand.Slug = GenerateSlug(createBrandDto.Name);
        brand.LogoUrl = createBrandDto.LogoUrl;
        brand.IsActive = true; // Explicitly set to true
        brand.SortOrder = createBrandDto.SortOrder;
        brand.CreatedAt = DateTime.UtcNow;
        brand.UpdatedAt = null;

        // Ensure all required fields are explicitly set
        if (string.IsNullOrEmpty(brand.Name))
            throw new ArgumentException("Brand name cannot be empty");
        
        if (string.IsNullOrEmpty(brand.Slug))
            throw new ArgumentException("Brand slug cannot be empty");

        // Debug: Log the values being set
        Console.WriteLine($"Creating brand: Name={brand.Name}, Slug={brand.Slug}, IsActive={brand.IsActive}, SortOrder={brand.SortOrder}");

        // Use a different approach - create the brand with all fields explicitly set
        var brandToSave = new Brand
        {
            Id = brand.Id,
            Name = brand.Name,
            Slug = brand.Slug,
            LogoUrl = brand.LogoUrl,
            IsActive = true, // Explicitly set again
            SortOrder = brand.SortOrder,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = null
        };

        // Force Entity Framework to recognize all changes
        await _unitOfWork.Repository<Brand>().AddAsync(brandToSave, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var brandDto = _mapper.Map<BrandDto>(brandToSave);
        
        // Calculate product count (will be 0 for new brand)
        brandDto.ProductCount = 0;
        
        return brandDto;
    }

    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace(",", "-")
            .Replace("(", "-")
            .Replace(")", "-")
            .Replace("[", "-")
            .Replace("]", "-")
            .Replace("{", "-")
            .Replace("}", "-")
            .Replace("&", "and")
            .Replace("@", "at")
            .Replace("#", "hash")
            .Replace("$", "dollar")
            .Replace("%", "percent")
            .Replace("^", "caret")
            .Replace("*", "star")
            .Replace("+", "plus")
            .Replace("=", "equals")
            .Replace("?", "question")
            .Replace("!", "exclamation")
            .Replace("|", "pipe")
            .Replace("\\", "-")
            .Replace("/", "-")
            .Replace(":", "-")
            .Replace(";", "-")
            .Replace("\"", "-")
            .Replace("'", "-")
            .Replace("<", "-")
            .Replace(">", "-")
            .Replace("~", "-")
            .Replace("`", "-")
            .Replace(" ", "-")
            .Trim('-')
            .Trim();
    }

    public async Task<BrandDto> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);
        if (brand == null)
            throw new ArgumentException("Brand not found.");

        _mapper.Map(updateBrandDto, brand);
        brand.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Brand>().Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var brandDto = _mapper.Map<BrandDto>(brand);
        
        // Calculate product count
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == id && p.IsActive);
        
        return brandDto;
    }

    public async Task<bool> DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);
        if (brand == null)
            return false;

        _unitOfWork.Repository<Brand>().Remove(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<AddBrandsResultDto> AddPredefinedBrandsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Predefined brands list
            var brandNames = new[]
            {
                "HEM", "HP", "Dell", "Xprinter", "Lenovo", "Acer", "Hikvision", 
                "Unv", "Canon", "LG", "WD", "SEAGATE", "Sunlux", "Datalogic"
            };

            // Get all existing brands for duplicate checking and sort order calculation
            var allBrands = await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken);
            
            // Get existing brand names (case-insensitive comparison)
            var existingBrandNames = allBrands
                .Where(b => brandNames.Any(bn => string.Equals(bn, b.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(b => b.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Get the maximum sort order to continue from there
            var maxSortOrder = allBrands.Any() ? allBrands.Max(b => b.SortOrder) : 0;

            var brandsToAdd = new List<Brand>();
            var sortOrder = maxSortOrder + 1;
            var skippedCount = 0;

            foreach (var brandName in brandNames)
            {
                // Skip if brand already exists (case-insensitive)
                if (existingBrandNames.Any(existing => string.Equals(existing, brandName, StringComparison.OrdinalIgnoreCase)))
                {
                    skippedCount++;
                    continue;
                }

                var brand = new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = brandName,
                    Slug = GenerateSlug(brandName),
                    IsActive = true,
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };
                brandsToAdd.Add(brand);
            }

            // Add only new brands to database using raw SQL to bypass EF configuration issues
            if (brandsToAdd.Any())
            {
                try
                {
                    foreach (var brand in brandsToAdd)
                    {
                        var sql = @"
                            INSERT INTO Brand (Id, Name, Slug, IsActive, SortOrder, CreatedAt, UpdatedAt) 
                            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})";

                        await _unitOfWork.ExecuteSqlRawAsync(sql, 
                            brand.Id,
                            brand.Name,
                            brand.Slug,
                            1, // IsActive = true (1 for bit type)
                            brand.SortOrder,
                            brand.CreatedAt,
                            (DateTime?)null); // UpdatedAt = NULL
                    }
                }
                catch (Exception dbEx)
                {
                    throw new InvalidOperationException($"Database error while adding brands: {dbEx.Message}. Inner exception: {dbEx.InnerException?.Message}", dbEx);
                }
            }

            return new AddBrandsResultDto
            {
                AddedCount = brandsToAdd.Count,
                SkippedCount = skippedCount,
                TotalRequested = brandNames.Length
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to add predefined brands: {ex.Message}", ex);
        }
    }

    public async Task<BrandDto> CreateBrandWithImageAsync(CreateBrandWithImageDto createBrandDto, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        // Validate image file
        if (!_fileUploadService.IsValidImageFile(imageFile))
        {
            throw new ArgumentException("Invalid image file format. Please upload a valid image file.");
        }

        // Upload the image
        var logoUrl = await _fileUploadService.UploadFileAsync(imageFile, "brands");

        // Create the brand with the uploaded logo
        var brandId = Guid.NewGuid();
        var brandName = createBrandDto.Name.Trim();
        var brandSlug = GenerateSlug(brandName);
        var createdAt = DateTime.UtcNow;

        // Ensure all required fields are explicitly set
        if (string.IsNullOrEmpty(brandName))
            throw new ArgumentException("Brand name cannot be empty");
        
        if (string.IsNullOrEmpty(brandSlug))
            throw new ArgumentException("Brand slug cannot be empty");

        // Use raw SQL to insert with explicit IsActive value
        var sql = @"
            INSERT INTO Brand (Id, Name, Slug, LogoUrl, IsActive, SortOrder, CreatedAt, UpdatedAt) 
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})";

        await _unitOfWork.ExecuteSqlRawAsync(sql, 
            brandId,
            brandName,
            brandSlug,
            logoUrl,
            1, // IsActive = true (1 for bit type)
            createBrandDto.SortOrder,
            createdAt,
            (DateTime?)null); // UpdatedAt = NULL

        // Retrieve the created brand
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(brandId, cancellationToken);
        if (brand == null)
            throw new InvalidOperationException("Failed to create brand");

        var brandDto = _mapper.Map<BrandDto>(brand);
        
        // Calculate product count (will be 0 for new brand)
        brandDto.ProductCount = 0;
        
        return brandDto;
    }

    public async Task<BrandDto> UpdateBrandWithImageAsync(Guid id, UpdateBrandWithImageDto updateBrandDto, IFormFile? imageFile, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id, cancellationToken);
        if (brand == null)
            throw new ArgumentException("Brand not found.");

        // Update basic brand information
        brand.Name = updateBrandDto.Name.Trim();
        brand.Slug = GenerateSlug(updateBrandDto.Name);
        brand.IsActive = updateBrandDto.IsActive;
        brand.SortOrder = updateBrandDto.SortOrder;
        brand.UpdatedAt = DateTime.UtcNow;

        // Handle image update if provided
        if (imageFile != null && imageFile.Length > 0)
        {
            // Validate image file
            if (!_fileUploadService.IsValidImageFile(imageFile))
            {
                throw new ArgumentException("Invalid image file format. Please upload a valid image file.");
            }

            // Upload the new image
            var logoUrl = await _fileUploadService.UploadFileAsync(imageFile, "brands");
            brand.LogoUrl = logoUrl;
        }

        _unitOfWork.Repository<Brand>().Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var brandDto = _mapper.Map<BrandDto>(brand);
        
        // Calculate product count
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        brandDto.ProductCount = products.Count(p => p.BrandId == id && p.IsActive);
        
        return brandDto;
    }

    public async Task<PagedResultDto<BrandDto>> GetBrandsPaginatedAsync(BrandPaginationRequestDto request, CancellationToken cancellationToken = default)
    {
        var brands = await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken);
        var filteredBrands = brands.Where(b => b.IsActive).AsQueryable();
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filteredBrands = filteredBrands.Where(b => b.Name.ToLower().Contains(searchTerm));
        }
        
        // Apply active status filter
        if (request.IsActive.HasValue)
        {
            filteredBrands = filteredBrands.Where(b => b.IsActive == request.IsActive.Value);
        }
        
        // Apply sorting
        filteredBrands = ApplyBrandSorting(filteredBrands, request.SortBy, request.SortOrder);
        
        // Get total count before pagination
        var totalCount = filteredBrands.Count();
        
        // Apply pagination
        var pagedBrands = filteredBrands
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Map to DTOs
        var brandDtos = _mapper.Map<IEnumerable<BrandDto>>(pagedBrands);
        
        // Get all products to calculate counts
        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var activeProducts = products.Where(p => p.IsActive);
        
        // Calculate product count for each brand
        foreach (var brandDto in brandDtos)
        {
            brandDto.ProductCount = activeProducts.Count(p => p.BrandId == brandDto.Id);
        }
        
        return CreatePagedResult(brandDtos, request.Page, request.PageSize, totalCount);
    }

    private IQueryable<Brand> ApplyBrandSorting(IQueryable<Brand> brands, string? sortBy, string sortOrder)
    {
        return sortBy?.ToLower() switch
        {
            "name" => sortOrder.ToLower() == "desc" 
                ? brands.OrderByDescending(b => b.Name)
                : brands.OrderBy(b => b.Name),
            "sortorder" => sortOrder.ToLower() == "desc"
                ? brands.OrderByDescending(b => b.SortOrder)
                : brands.OrderBy(b => b.SortOrder),
            "createdat" => sortOrder.ToLower() == "desc"
                ? brands.OrderByDescending(b => b.CreatedAt)
                : brands.OrderBy(b => b.CreatedAt),
            _ => brands.OrderBy(b => b.SortOrder)
        };
    }

    private PagedResultDto<BrandDto> CreatePagedResult(IEnumerable<BrandDto> items, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        return new PagedResultDto<BrandDto>
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
}
