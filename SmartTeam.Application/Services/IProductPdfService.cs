using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IProductPdfService
{
    Task<ProductPdfDto> UploadPdfAsync(CreateProductPdfDto createDto, Guid userId, CancellationToken cancellationToken = default);
    Task<ProductPdfDto?> GetPdfByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductPdfDto?> GetPdfByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductPdfDto>> GetAllPdfsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductPdfDto>> GetActivePdfsAsync(CancellationToken cancellationToken = default);
    Task<PagedProductPdfResultDto> SearchPdfsAsync(ProductPdfSearchDto searchDto, CancellationToken cancellationToken = default);
    Task<ProductPdfDto?> UpdatePdfAsync(Guid id, UpdateProductPdfDto updateDto, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeletePdfAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TogglePdfStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductPdfDownloadResponseDto?> DownloadPdfAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductPdfDownloadResponseDto?> DownloadPdfByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> HasPdfAsync(Guid productId, CancellationToken cancellationToken = default);
}
