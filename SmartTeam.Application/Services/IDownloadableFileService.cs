using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IDownloadableFileService
{
    Task<DownloadableFileDto> UploadFileAsync(CreateDownloadableFileDto createDto, Guid userId, CancellationToken cancellationToken = default);
    Task<DownloadableFileDto?> GetFileByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadableFileDto>> GetAllFilesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadableFileDto>> GetActiveFilesAsync(CancellationToken cancellationToken = default);
    Task<PagedDownloadableFileResultDto> SearchFilesAsync(DownloadableFileSearchDto searchDto, CancellationToken cancellationToken = default);
    Task<DownloadableFileDto?> UpdateFileAsync(Guid id, UpdateDownloadableFileDto updateDto, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ToggleFileStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FileDownloadResponseDto?> DownloadFileAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DownloadableFileStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadableFileDto>> GetFilesByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
