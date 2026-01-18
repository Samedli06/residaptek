using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IFilterService
{
    // Filter management
    Task<IEnumerable<FilterDto>> GetAllFiltersAsync(CancellationToken cancellationToken = default);
    Task<PagedFilterResultDto> SearchFiltersAsync(FilterSearchDto searchDto, CancellationToken cancellationToken = default);
    Task<FilterDto?> GetFilterByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FilterDto?> GetFilterBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<FilterDto> CreateFilterAsync(CreateFilterDto createFilterDto, CancellationToken cancellationToken = default);
    Task<FilterDto> UpdateFilterAsync(Guid id, UpdateFilterDto updateFilterDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteFilterAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Filter option management
    Task<FilterOptionDto> AddFilterOptionAsync(Guid filterId, CreateFilterOptionDto createOptionDto, CancellationToken cancellationToken = default);
    Task<FilterOptionDto> UpdateFilterOptionAsync(Guid filterId, Guid optionId, UpdateFilterOptionDto updateOptionDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteFilterOptionAsync(Guid filterId, Guid optionId, CancellationToken cancellationToken = default);
    Task<bool> ReorderFilterOptionsAsync(Guid filterId, List<Guid> optionIds, CancellationToken cancellationToken = default);
    
    // Product attribute management
    Task<IEnumerable<ProductAttributeValueDto>> GetProductAttributesAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductAttributeValueDto> AssignFilterToProductAsync(AssignFilterToProductDto assignDto, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductAttributeValueDto>> BulkAssignFilterToProductsAsync(BulkAssignFilterDto bulkAssignDto, CancellationToken cancellationToken = default);
    Task<bool> RemoveFilterFromProductAsync(Guid productId, Guid filterId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAllFiltersFromProductAsync(Guid productId, CancellationToken cancellationToken = default);
    
    // Statistics and utility
    Task<FilterStatisticsDto> GetFilterStatisticsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FilterTypeDto>> GetFilterTypesAsync();
    Task<bool> ReorderFiltersAsync(List<Guid> filterIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<FilterDto>> GetFiltersForProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FilterDto>> GetAvailableFiltersForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    
    // Public filter methods for frontend
    Task<IEnumerable<FilterDto>> GetPublicFiltersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FilterDto>> GetPublicFiltersForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
