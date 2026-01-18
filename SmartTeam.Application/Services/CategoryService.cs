using AutoMapper;
using Microsoft.AspNetCore.Http;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileUploadService _fileUploadService;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, IFileUploadService fileUploadService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileUploadService = fileUploadService;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(categories.Where(c => c.IsActive));
    }

    public async Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        // Get all categories to build the hierarchy
        var allCategories = await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken);
        var activeCategories = allCategories.Where(c => c.IsActive).ToList();
        
        // Build parent-child relationships in memory
        var categoryDict = activeCategories.ToDictionary(c => c.Id, c => c);
        
        // Clear existing subcategories to avoid duplicates
        foreach (var category in activeCategories)
        {
            category.SubCategories.Clear();
        }
        
        // Build the hierarchy
        foreach (var category in activeCategories)
        {
            if (category.ParentCategoryId.HasValue && categoryDict.ContainsKey(category.ParentCategoryId.Value))
            {
                var parent = categoryDict[category.ParentCategoryId.Value];
                // Only add if not already present (avoid duplicates)
                if (!parent.SubCategories.Any(sc => sc.Id == category.Id))
                {
                    parent.SubCategories.Add(category);
                }
            }
        }
        
        // Sort subcategories by SortOrder for each parent
        foreach (var category in activeCategories.Where(c => c.ParentCategoryId == null))
        {
            SortSubCategoriesRecursively(category);
        }
        
        // Get only root categories (those without parents)
        var rootCategories = activeCategories.Where(c => c.ParentCategoryId == null).OrderBy(c => c.SortOrder);
        
        return _mapper.Map<IEnumerable<CategoryDto>>(rootCategories);
    }
    
    private void SortSubCategoriesRecursively(Category category)
    {
        if (category.SubCategories.Any())
        {
            // Sort current level subcategories
            var sortedSubCategories = category.SubCategories.OrderBy(sc => sc.SortOrder).ToList();
            category.SubCategories.Clear();
            foreach (var subCategory in sortedSubCategories)
            {
                category.SubCategories.Add(subCategory);
                // Recursively sort deeper levels
                SortSubCategoriesRecursively(subCategory);
            }
        }
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var allCategories = await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken);
        var activeCategories = allCategories.Where(c => c.IsActive).ToList();
        
        var category = activeCategories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            return null;

        var categoryDict = activeCategories.ToDictionary(c => c.Id, c => c);
        
        foreach (var cat in activeCategories)
        {
            cat.SubCategories.Clear();
        }
        
        foreach (var cat in activeCategories)
        {
            if (cat.ParentCategoryId.HasValue && categoryDict.ContainsKey(cat.ParentCategoryId.Value))
            {
                var parent = categoryDict[cat.ParentCategoryId.Value];
                if (!parent.SubCategories.Any(sc => sc.Id == cat.Id))
                {
                    parent.SubCategories.Add(cat);
                }
            }
        }
        
        SortSubCategoriesRecursively(category);
        
        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var allCategories = await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken);
        var activeCategories = allCategories.Where(c => c.IsActive).ToList();
        
        var category = activeCategories.FirstOrDefault(c => c.Slug == slug);
        if (category == null)
            return null;

        var categoryDict = activeCategories.ToDictionary(c => c.Id, c => c);
        
        foreach (var cat in activeCategories)
        {
            cat.SubCategories.Clear();
        }
        
        foreach (var cat in activeCategories)
        {
            if (cat.ParentCategoryId.HasValue && categoryDict.ContainsKey(cat.ParentCategoryId.Value))
            {
                var parent = categoryDict[cat.ParentCategoryId.Value];
                if (!parent.SubCategories.Any(sc => sc.Id == cat.Id))
                {
                    parent.SubCategories.Add(cat);
                }
            }
        }
        
        SortSubCategoriesRecursively(category);
        
        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default)
    {
        var category = _mapper.Map<Category>(createCategoryDto);
        
        if (createCategoryDto.ParentCategoryId.HasValue)
        {
            var parentExists = await _unitOfWork.Repository<Category>()
                .AnyAsync(c => c.Id == createCategoryDto.ParentCategoryId.Value && c.IsActive, cancellationToken);
            
            if (!parentExists)
            {
                throw new ArgumentException("Parent category not found or inactive.");
            }
        }

        await _unitOfWork.Repository<Category>().AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto updateCategoryDto, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new ArgumentException("Category not found.");
        }

        if (updateCategoryDto.ParentCategoryId.HasValue && updateCategoryDto.ParentCategoryId != category.ParentCategoryId)
        {
            var parentExists = await _unitOfWork.Repository<Category>()
                .AnyAsync(c => c.Id == updateCategoryDto.ParentCategoryId.Value && c.IsActive, cancellationToken);
            
            if (!parentExists)
            {
                throw new ArgumentException("Parent category not found or inactive.");
            }

            if (updateCategoryDto.ParentCategoryId == id)
            {
                throw new ArgumentException("Category cannot be its own parent.");
            }
        }

        _mapper.Map(updateCategoryDto, category);
        _unitOfWork.Repository<Category>().Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateCategoryWithImageAsync(Guid id, UpdateCategoryWithImageDto updateCategoryDto, IFormFile? imageFile, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new ArgumentException("Category not found.");
        }

        if (updateCategoryDto.ParentCategoryId.HasValue && updateCategoryDto.ParentCategoryId != category.ParentCategoryId)
        {
            var parentExists = await _unitOfWork.Repository<Category>()
                .AnyAsync(c => c.Id == updateCategoryDto.ParentCategoryId.Value && c.IsActive, cancellationToken);
            
            if (!parentExists)
            {
                throw new ArgumentException("Parent category not found or inactive.");
            }

            if (updateCategoryDto.ParentCategoryId == id)
            {
                throw new ArgumentException("Category cannot be its own parent.");
            }
        }

        // Update category properties
        category.Name = updateCategoryDto.Name;
        category.Description = updateCategoryDto.Description;
        category.IsActive = updateCategoryDto.IsActive;
        category.SortOrder = updateCategoryDto.SortOrder;
        category.ParentCategoryId = updateCategoryDto.ParentCategoryId;
        category.UpdatedAt = DateTime.UtcNow;

        // Update image if provided
        if (imageFile != null && imageFile.Length > 0)
        {
            // IMPORTANT: Do not delete old images to preserve them after publish
            // Delete old image if exists
            // if (!string.IsNullOrEmpty(category.ImageUrl))
            // {
            //     await _fileUploadService.DeleteFileAsync(category.ImageUrl);
            // }

            // Upload new image
            var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "categories");
            category.ImageUrl = imageUrl;
        }

        _unitOfWork.Repository<Category>().Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return false;
        }

        var hasProducts = await _unitOfWork.Repository<Product>()
            .AnyAsync(p => p.CategoryId == id, cancellationToken);
        
        if (hasProducts)
        {
            throw new InvalidOperationException("Cannot delete category that has products.");
        }

        var hasSubCategories = await _unitOfWork.Repository<Category>()
            .AnyAsync(c => c.ParentCategoryId == id && c.IsActive, cancellationToken);
        
        if (hasSubCategories)
        {
            throw new InvalidOperationException("Cannot delete category that has subcategories.");
        }

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        
        _unitOfWork.Repository<Category>().Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var subCategories = await _unitOfWork.Repository<Category>()
            .FindAsync(c => c.ParentCategoryId == parentId && c.IsActive, cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(subCategories);
    }

    public async Task<CategoryDto> CreateCategoryWithImageAsync(CreateCategoryWithImageDto createCategoryDto, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            throw new ArgumentException("Image file is required.");
        }

        if (createCategoryDto.ParentCategoryId.HasValue)
        {
            var parentExists = await _unitOfWork.Repository<Category>()
                .AnyAsync(c => c.Id == createCategoryDto.ParentCategoryId.Value && c.IsActive, cancellationToken);
            
            if (!parentExists)
            {
                throw new ArgumentException("Parent category not found or inactive.");
            }
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = createCategoryDto.Name,
            Slug = GenerateSlug(createCategoryDto.Name),
            Description = createCategoryDto.Description,
            SortOrder = createCategoryDto.SortOrder,
            ParentCategoryId = createCategoryDto.ParentCategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Upload image
        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "categories");
        category.ImageUrl = imageUrl;

        await _unitOfWork.Repository<Category>().AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UploadCategoryImageAsync(Guid categoryId, IFormFile imageFile, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
        {
            throw new ArgumentException("Category not found");
        }

        // IMPORTANT: Do not delete old images to preserve them after publish
        // Delete old image if exists
        // if (!string.IsNullOrEmpty(category.ImageUrl))
        // {
        //     await _fileUploadService.DeleteFileAsync(category.ImageUrl);
        // }

        // Upload new image
        var imageUrl = await _fileUploadService.UploadFileAsync(imageFile, "categories");
        category.ImageUrl = imageUrl;
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Category>().Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<bool> DeleteCategoryImageAsync(Guid categoryId, string imageUrl, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
        {
            return false;
        }

        // Delete the file
        var deleted = await _fileUploadService.DeleteFileAsync(imageUrl);
        
        if (deleted && category.ImageUrl == imageUrl)
        {
            category.ImageUrl = null;
            category.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return deleted;
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
}
