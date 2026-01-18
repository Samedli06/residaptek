using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class FilterService : IFilterService
{
    private readonly IRepository<Filter> _filterRepository;
    private readonly IRepository<FilterOption> _filterOptionRepository;
    private readonly IRepository<ProductAttributeValue> _attributeRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FilterService(
        IRepository<Filter> filterRepository,
        IRepository<FilterOption> filterOptionRepository,
        IRepository<ProductAttributeValue> attributeRepository,
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _filterRepository = filterRepository;
        _filterOptionRepository = filterOptionRepository;
        _attributeRepository = attributeRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<FilterDto>> GetAllFiltersAsync(CancellationToken cancellationToken = default)
    {
        var filters = await _filterRepository.GetAllWithIncludesAsync(f => f.Options);
        return _mapper.Map<IEnumerable<FilterDto>>(filters.OrderBy(f => f.SortOrder));
    }

    public async Task<PagedFilterResultDto> SearchFiltersAsync(FilterSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        var query = _filterRepository.GetAllWithIncludesAsync(f => f.Options).Result.AsQueryable();

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLower();
            query = query.Where(f => f.Name.ToLower().Contains(searchTerm) || 
                                   f.Slug.ToLower().Contains(searchTerm));
        }

        if (searchDto.Type.HasValue)
        {
            query = query.Where(f => f.Type == searchDto.Type.Value);
        }

        if (searchDto.IsActive.HasValue)
        {
            query = query.Where(f => f.IsActive == searchDto.IsActive.Value);
        }

        // Apply sorting
        query = searchDto.SortBy?.ToLower() switch
        {
            "name" => searchDto.SortOrder == "desc" ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
            "type" => searchDto.SortOrder == "desc" ? query.OrderByDescending(f => f.Type) : query.OrderBy(f => f.Type),
            "createdat" => searchDto.SortOrder == "desc" ? query.OrderByDescending(f => f.CreatedAt) : query.OrderBy(f => f.CreatedAt),
            _ => searchDto.SortOrder == "desc" ? query.OrderByDescending(f => f.SortOrder) : query.OrderBy(f => f.SortOrder)
        };

        var totalCount = query.Count();
        var filters = query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();

        return new PagedFilterResultDto
        {
            Filters = _mapper.Map<IEnumerable<FilterDto>>(filters),
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize),
            HasNextPage = searchDto.Page * searchDto.PageSize < totalCount,
            HasPreviousPage = searchDto.Page > 1
        };
    }

    public async Task<FilterDto?> GetFilterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = await _filterRepository.GetByIdWithIncludesAsync(id, f => f.Options);
        return filter != null ? _mapper.Map<FilterDto>(filter) : null;
    }

    public async Task<FilterDto?> GetFilterBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var filter = await _filterRepository.FirstOrDefaultWithIncludesAsync(
            f => f.Slug == slug, f => f.Options);
        return filter != null ? _mapper.Map<FilterDto>(filter) : null;
    }

    public async Task<FilterDto> CreateFilterAsync(CreateFilterDto createFilterDto, CancellationToken cancellationToken = default)
    {
        // Check if filter with same name exists
        var existingFilter = await _filterRepository.FirstOrDefaultAsync(f => f.Name == createFilterDto.Name, cancellationToken);
        if (existingFilter != null)
        {
            throw new InvalidOperationException($"Filter with name '{createFilterDto.Name}' already exists.");
        }

        // Generate slug and check if filter with same slug exists
        var slug = GenerateSlug(createFilterDto.Name);
        var existingFilterBySlug = await _filterRepository.FirstOrDefaultAsync(f => f.Slug == slug, cancellationToken);
        if (existingFilterBySlug != null)
        {
            throw new InvalidOperationException($"Filter with slug '{slug}' already exists.");
        }

        var filter = _mapper.Map<Filter>(createFilterDto);
        filter.Id = Guid.NewGuid();
        filter.Slug = slug;
        filter.CreatedAt = DateTime.UtcNow;

        // Create filter options
        foreach (var optionDto in createFilterDto.Options)
        {
            var option = _mapper.Map<FilterOption>(optionDto);
            option.Id = Guid.NewGuid();
            option.FilterId = filter.Id;
            option.CreatedAt = DateTime.UtcNow;
            filter.Options.Add(option);
        }

        await _filterRepository.AddAsync(filter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FilterDto>(filter);
    }

    public async Task<FilterDto> UpdateFilterAsync(Guid id, UpdateFilterDto updateFilterDto, CancellationToken cancellationToken = default)
    {
        var filter = await _filterRepository.GetByIdAsync(id, cancellationToken);
        if (filter == null)
        {
            throw new ArgumentException($"Filter with ID {id} not found.");
        }

        // Check if another filter with same name exists
        var existingFilter = await _filterRepository.FirstOrDefaultAsync(
            f => f.Name == updateFilterDto.Name && f.Id != id, cancellationToken);
        if (existingFilter != null)
        {
            throw new InvalidOperationException($"Another filter with name '{updateFilterDto.Name}' already exists.");
        }

        // Generate slug and check if another filter with same slug exists
        var slug = GenerateSlug(updateFilterDto.Name);
        var existingFilterBySlug = await _filterRepository.FirstOrDefaultAsync(
            f => f.Slug == slug && f.Id != id, cancellationToken);
        if (existingFilterBySlug != null)
        {
            throw new InvalidOperationException($"Another filter with slug '{slug}' already exists.");
        }

        _mapper.Map(updateFilterDto, filter);
        filter.Slug = slug;
        filter.UpdatedAt = DateTime.UtcNow;

        _filterRepository.Update(filter);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FilterDto>(filter);
    }

    public async Task<bool> DeleteFilterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = await _filterRepository.GetByIdWithIncludesAsync(id, f => f.Options, f => f.ProductAttributeValues);
        if (filter == null)
        {
            return false;
        }

        // Check if filter is being used by products
        if (filter.ProductAttributeValues.Any())
        {
            throw new InvalidOperationException("Cannot delete filter that is assigned to products. Remove it from all products first.");
        }

        // Remove all filter options
        if (filter.Options.Any())
        {
            _filterOptionRepository.RemoveRange(filter.Options);
        }

        _filterRepository.Remove(filter);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<FilterOptionDto> AddFilterOptionAsync(Guid filterId, CreateFilterOptionDto createOptionDto, CancellationToken cancellationToken = default)
    {
        var filter = await _filterRepository.GetByIdAsync(filterId, cancellationToken);
        if (filter == null)
        {
            throw new ArgumentException($"Filter with ID {filterId} not found.");
        }

        // Check if option with same value exists for this filter
        var existingOption = await _filterOptionRepository.FirstOrDefaultAsync(
            o => o.FilterId == filterId && o.Value == createOptionDto.Value, cancellationToken);
        if (existingOption != null)
        {
            throw new InvalidOperationException($"Filter option with value '{createOptionDto.Value}' already exists for this filter.");
        }

        var option = _mapper.Map<FilterOption>(createOptionDto);
        option.Id = Guid.NewGuid();
        option.FilterId = filterId;
        option.CreatedAt = DateTime.UtcNow;

        await _filterOptionRepository.AddAsync(option, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FilterOptionDto>(option);
    }

    public async Task<FilterOptionDto> UpdateFilterOptionAsync(Guid filterId, Guid optionId, UpdateFilterOptionDto updateOptionDto, CancellationToken cancellationToken = default)
    {
        var option = await _filterOptionRepository.FirstOrDefaultAsync(
            o => o.Id == optionId && o.FilterId == filterId, cancellationToken);
        if (option == null)
        {
            throw new ArgumentException($"Filter option with ID {optionId} not found for filter {filterId}.");
        }

        // Check if another option with same value exists for this filter
        var existingOption = await _filterOptionRepository.FirstOrDefaultAsync(
            o => o.FilterId == filterId && o.Value == updateOptionDto.Value && o.Id != optionId, cancellationToken);
        if (existingOption != null)
        {
            throw new InvalidOperationException($"Another filter option with value '{updateOptionDto.Value}' already exists for this filter.");
        }

        _mapper.Map(updateOptionDto, option);
        _filterOptionRepository.Update(option);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FilterOptionDto>(option);
    }

    public async Task<bool> DeleteFilterOptionAsync(Guid filterId, Guid optionId, CancellationToken cancellationToken = default)
    {
        var option = await _filterOptionRepository.FirstOrDefaultAsync(
            o => o.Id == optionId && o.FilterId == filterId, cancellationToken);
        if (option == null)
        {
            return false;
        }

        // Check if option is being used by products
        var isUsed = await _attributeRepository.AnyAsync(
            a => a.FilterOptionId == optionId, cancellationToken);
        if (isUsed)
        {
            throw new InvalidOperationException("Cannot delete filter option that is assigned to products. Remove it from all products first.");
        }

        _filterOptionRepository.Remove(option);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ReorderFilterOptionsAsync(Guid filterId, List<Guid> optionIds, CancellationToken cancellationToken = default)
    {
        var options = await _filterOptionRepository.FindAsync(o => o.FilterId == filterId, cancellationToken);
        var optionsList = options.ToList();

        for (int i = 0; i < optionIds.Count; i++)
        {
            var option = optionsList.FirstOrDefault(o => o.Id == optionIds[i]);
            if (option != null)
            {
                option.SortOrder = i;
            }
        }

        _filterOptionRepository.UpdateRange(optionsList);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<ProductAttributeValueDto>> GetProductAttributesAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var attributes = await _attributeRepository.FindAsync(a => a.ProductId == productId, cancellationToken);
        var attributesWithIncludes = new List<ProductAttributeValue>();

        foreach (var attr in attributes)
        {
            var fullAttr = await _attributeRepository.FirstOrDefaultWithIncludesAsync(
                a => a.Id == attr.Id, 
                a => a.Filter, 
                a => a.FilterOption,
                a => a.Product);
            if (fullAttr != null)
            {
                attributesWithIncludes.Add(fullAttr);
            }
        }

        return _mapper.Map<IEnumerable<ProductAttributeValueDto>>(attributesWithIncludes);
    }

    public async Task<ProductAttributeValueDto> AssignFilterToProductAsync(AssignFilterToProductDto assignDto, CancellationToken cancellationToken = default)
    {
        // Validate product exists
        var product = await _productRepository.GetByIdAsync(assignDto.ProductId, cancellationToken);
        if (product == null)
        {
            throw new ArgumentException($"Product with ID {assignDto.ProductId} not found.");
        }

        // Validate filter exists
        var filter = await _filterRepository.GetByIdAsync(assignDto.FilterId, cancellationToken);
        if (filter == null)
        {
            throw new ArgumentException($"Filter with ID {assignDto.FilterId} not found.");
        }

        // Validate filter option if provided
        if (assignDto.FilterOptionId.HasValue)
        {
            var filterOption = await _filterOptionRepository.FirstOrDefaultAsync(
                o => o.Id == assignDto.FilterOptionId.Value && o.FilterId == assignDto.FilterId, cancellationToken);
            if (filterOption == null)
            {
                throw new ArgumentException($"Filter option with ID {assignDto.FilterOptionId} not found for filter {assignDto.FilterId}.");
            }
        }

        // Check if assignment already exists
        var existingAttribute = await _attributeRepository.FirstOrDefaultAsync(
            a => a.ProductId == assignDto.ProductId && a.FilterId == assignDto.FilterId, cancellationToken);
        
        if (existingAttribute != null)
        {
            // Update existing assignment
            existingAttribute.FilterOptionId = assignDto.FilterOptionId;
            existingAttribute.CustomValue = assignDto.CustomValue;
            _attributeRepository.Update(existingAttribute);
        }
        else
        {
            // Create new assignment
            existingAttribute = new ProductAttributeValue
            {
                Id = Guid.NewGuid(),
                ProductId = assignDto.ProductId,
                FilterId = assignDto.FilterId,
                FilterOptionId = assignDto.FilterOptionId,
                CustomValue = assignDto.CustomValue,
                CreatedAt = DateTime.UtcNow
            };
            await _attributeRepository.AddAsync(existingAttribute, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var fullAttribute = await _attributeRepository.FirstOrDefaultWithIncludesAsync(
            a => a.Id == existingAttribute.Id,
            a => a.Filter,
            a => a.FilterOption,
            a => a.Product);

        return _mapper.Map<ProductAttributeValueDto>(fullAttribute);
    }

    public async Task<IEnumerable<ProductAttributeValueDto>> BulkAssignFilterToProductsAsync(BulkAssignFilterDto bulkAssignDto, CancellationToken cancellationToken = default)
    {
        var results = new List<ProductAttributeValueDto>();

        foreach (var productId in bulkAssignDto.ProductIds)
        {
            try
            {
                var assignDto = new AssignFilterToProductDto
                {
                    ProductId = productId,
                    FilterId = bulkAssignDto.FilterId,
                    FilterOptionId = bulkAssignDto.FilterOptionId,
                    CustomValue = bulkAssignDto.CustomValue
                };

                var result = await AssignFilterToProductAsync(assignDto, cancellationToken);
                results.Add(result);
            }
            catch (ArgumentException)
            {
                // Skip invalid products/filters
                continue;
            }
        }

        return results;
    }

    public async Task<bool> RemoveFilterFromProductAsync(Guid productId, Guid filterId, CancellationToken cancellationToken = default)
    {
        var attribute = await _attributeRepository.FirstOrDefaultAsync(
            a => a.ProductId == productId && a.FilterId == filterId, cancellationToken);
        
        if (attribute == null)
        {
            return false;
        }

        _attributeRepository.Remove(attribute);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RemoveAllFiltersFromProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var attributes = await _attributeRepository.FindAsync(a => a.ProductId == productId, cancellationToken);
        
        if (!attributes.Any())
        {
            return false;
        }

        _attributeRepository.RemoveRange(attributes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<FilterStatisticsDto> GetFilterStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var filters = await _filterRepository.GetAllAsync(cancellationToken);
        var filterOptions = await _filterOptionRepository.GetAllAsync(cancellationToken);
        var attributeValues = await _attributeRepository.GetAllAsync(cancellationToken);

        var filtersList = filters.ToList();

        return new FilterStatisticsDto
        {
            TotalFilters = filtersList.Count,
            ActiveFilters = filtersList.Count(f => f.IsActive),
            InactiveFilters = filtersList.Count(f => !f.IsActive),
            FiltersByType = filtersList.GroupBy(f => f.Type).ToDictionary(g => g.Key, g => g.Count()),
            TotalFilterOptions = filterOptions.Count(),
            ProductsWithFilters = attributeValues.Select(a => a.ProductId).Distinct().Count()
        };
    }

    public async Task<IEnumerable<FilterTypeDto>> GetFilterTypesAsync()
    {
        var filterTypes = Enum.GetValues<FilterType>().Select(type => new FilterTypeDto
        {
            Value = type,
            Name = type.ToString(),
            Description = type switch
            {
                FilterType.Select => "Single selection dropdown (e.g., Brand, Size)",
                FilterType.MultiSelect => "Multiple selection checkboxes (e.g., Features, Tags)",
                FilterType.Range => "Range slider (e.g., Price range, Rating)",
                FilterType.Text => "Text input field (e.g., Custom text)",
                FilterType.Color => "Color picker (e.g., Product colors)",
                _ => type.ToString()
            }
        });

        return await Task.FromResult(filterTypes);
    }

    public async Task<bool> ReorderFiltersAsync(List<Guid> filterIds, CancellationToken cancellationToken = default)
    {
        var filters = await _filterRepository.GetAllAsync(cancellationToken);
        var filtersList = filters.ToList();

        for (int i = 0; i < filterIds.Count; i++)
        {
            var filter = filtersList.FirstOrDefault(f => f.Id == filterIds[i]);
            if (filter != null)
            {
                filter.SortOrder = i;
                filter.UpdatedAt = DateTime.UtcNow;
            }
        }

        _filterRepository.UpdateRange(filtersList.Where(f => filterIds.Contains(f.Id)));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<FilterDto>> GetFiltersForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var attributes = await _attributeRepository.FindAsync(a => a.ProductId == productId, cancellationToken);
        var filterIds = attributes.Select(a => a.FilterId).Distinct().ToList();

        var filters = new List<Filter>();
        foreach (var filterId in filterIds)
        {
            var filter = await _filterRepository.GetByIdWithIncludesAsync(filterId, f => f.Options);
            if (filter != null)
            {
                filters.Add(filter);
            }
        }

        return _mapper.Map<IEnumerable<FilterDto>>(filters.OrderBy(f => f.SortOrder));
    }

    public async Task<IEnumerable<FilterDto>> GetAvailableFiltersForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        // Get all products in the category
        var products = await _productRepository.FindAsync(p => p.CategoryId == categoryId, cancellationToken);
        var productIds = products.Select(p => p.Id).ToList();

        // Get all filters used by products in this category
        var attributes = await _attributeRepository.FindAsync(a => productIds.Contains(a.ProductId), cancellationToken);
        var filterIds = attributes.Select(a => a.FilterId).Distinct().ToList();

        var filters = new List<Filter>();
        foreach (var filterId in filterIds)
        {
            var filter = await _filterRepository.GetByIdWithIncludesAsync(filterId, f => f.Options);
            if (filter != null && filter.IsActive)
            {
                filters.Add(filter);
            }
        }

        return _mapper.Map<IEnumerable<FilterDto>>(filters.OrderBy(f => f.SortOrder));
    }

    public async Task<IEnumerable<FilterDto>> GetPublicFiltersAsync(CancellationToken cancellationToken = default)
    {
        var filters = await _filterRepository.GetAllWithIncludesAsync(f => f.Options);
        var activeFilters = filters.Where(f => f.IsActive).OrderBy(f => f.SortOrder);
        return _mapper.Map<IEnumerable<FilterDto>>(activeFilters);
    }

    public async Task<IEnumerable<FilterDto>> GetPublicFiltersForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        // Get all products in the category and its subcategories
        var categoryIds = await GetCategoryIdsIncludingSubcategories(categoryId, cancellationToken);
        var products = await _productRepository.FindAsync(p => categoryIds.Contains(p.CategoryId) && p.IsActive, cancellationToken);
        var productIds = products.Select(p => p.Id).ToList();

        if (!productIds.Any())
        {
            return new List<FilterDto>();
        }

        // Get all filters used by products in this category
        var attributes = await _attributeRepository.FindAsync(a => productIds.Contains(a.ProductId), cancellationToken);
        var filterIds = attributes.Select(a => a.FilterId).Distinct().ToList();

        var filters = new List<Filter>();
        foreach (var filterId in filterIds)
        {
            var filter = await _filterRepository.GetByIdWithIncludesAsync(filterId, f => f.Options);
            if (filter != null && filter.IsActive)
            {
                filters.Add(filter);
            }
        }

        return _mapper.Map<IEnumerable<FilterDto>>(filters.OrderBy(f => f.SortOrder));
    }

    private async Task<List<Guid>> GetCategoryIdsIncludingSubcategories(Guid categoryId, CancellationToken cancellationToken)
    {
        var categoryIds = new List<Guid> { categoryId };
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        // Get all subcategories recursively
        await GetSubcategoryIdsRecursive(categoryId, categoryIds, categoryRepository, cancellationToken);
        
        return categoryIds;
    }

    private async Task GetSubcategoryIdsRecursive(Guid parentCategoryId, List<Guid> categoryIds, IRepository<Category> categoryRepository, CancellationToken cancellationToken)
    {
        var subcategories = await categoryRepository.FindAsync(c => c.ParentCategoryId == parentCategoryId, cancellationToken);
        
        foreach (var subcategory in subcategories)
        {
            if (!categoryIds.Contains(subcategory.Id))
            {
                categoryIds.Add(subcategory.Id);
                // Recursively get subcategories of this subcategory
                await GetSubcategoryIdsRecursive(subcategory.Id, categoryIds, categoryRepository, cancellationToken);
            }
        }
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
