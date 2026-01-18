using AutoMapper;
using Microsoft.Extensions.Logging;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using System.Linq.Expressions;

namespace SmartTeam.Application.Services;

public class DownloadableFileService : IDownloadableFileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileUploadService _fileUploadService;
    private readonly IMapper _mapper;
    private readonly ILogger<DownloadableFileService> _logger;

    public DownloadableFileService(
        IUnitOfWork unitOfWork,
        IFileUploadService fileUploadService,
        IMapper mapper,
        ILogger<DownloadableFileService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileUploadService = fileUploadService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DownloadableFileDto> UploadFileAsync(CreateDownloadableFileDto createDto, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Upload the file
            var filePath = await _fileUploadService.UploadFileAsync(createDto.File, "downloads");
            
            // Determine the display filename
            var displayFileName = !string.IsNullOrWhiteSpace(createDto.CustomFileName) 
                ? createDto.CustomFileName.Trim()
                : createDto.File.FileName;
            
            // Create the entity
            var downloadableFile = new DownloadableFile
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(filePath), // Physical filename on disk
                OriginalFileName = displayFileName, // Display name for users
                FilePath = filePath,
                ContentType = createDto.File.ContentType,
                FileSize = createDto.File.Length,
                Description = createDto.Description,
                Category = createDto.Category,
                IsActive = createDto.IsActive,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<DownloadableFile>().AddAsync(downloadableFile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"File uploaded successfully: {downloadableFile.OriginalFileName} by user {userId}");

            return await GetFileByIdAsync(downloadableFile.Id, cancellationToken) ?? throw new InvalidOperationException("Failed to retrieve uploaded file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file: {createDto.File.FileName}");
            throw;
        }
    }

    public async Task<DownloadableFileDto?> GetFileByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _unitOfWork.Repository<DownloadableFile>().GetByIdWithIncludesAsync(id, 
            f => f.CreatedByUser, f => f.UpdatedByUser);

        if (file == null) return null;

        return MapToDto(file);
    }

    public async Task<IEnumerable<DownloadableFileDto>> GetAllFilesAsync(CancellationToken cancellationToken = default)
    {
        var files = await _unitOfWork.Repository<DownloadableFile>().GetAllWithIncludesAsync(
            f => f.CreatedByUser, f => f.UpdatedByUser);

        return files.Select(MapToDto).OrderByDescending(f => f.CreatedAt);
    }

    public async Task<IEnumerable<DownloadableFileDto>> GetActiveFilesAsync(CancellationToken cancellationToken = default)
    {
        var files = await _unitOfWork.Repository<DownloadableFile>().FindAsync(f => f.IsActive, cancellationToken);
        var filesWithUsers = new List<DownloadableFileDto>();

        foreach (var file in files)
        {
            var fileWithUsers = await _unitOfWork.Repository<DownloadableFile>().GetByIdWithIncludesAsync(file.Id, 
                f => f.CreatedByUser, f => f.UpdatedByUser);
            if (fileWithUsers != null)
                filesWithUsers.Add(MapToDto(fileWithUsers));
        }

        return filesWithUsers.OrderByDescending(f => f.CreatedAt);
    }

    public async Task<PagedDownloadableFileResultDto> SearchFilesAsync(DownloadableFileSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        // For simplicity, get all files and filter in memory
        var allFiles = await GetAllFilesAsync(cancellationToken);
        var filteredFiles = allFiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLower();
            filteredFiles = filteredFiles.Where(f => 
                f.OriginalFileName.ToLower().Contains(searchTerm) ||
                (f.Description != null && f.Description.ToLower().Contains(searchTerm)) ||
                (f.Category != null && f.Category.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Category))
        {
            filteredFiles = filteredFiles.Where(f => f.Category == searchDto.Category);
        }

        if (searchDto.IsActive.HasValue)
        {
            filteredFiles = filteredFiles.Where(f => f.IsActive == searchDto.IsActive.Value);
        }

        if (searchDto.CreatedFrom.HasValue)
        {
            filteredFiles = filteredFiles.Where(f => f.CreatedAt >= searchDto.CreatedFrom.Value);
        }

        if (searchDto.CreatedTo.HasValue)
        {
            filteredFiles = filteredFiles.Where(f => f.CreatedAt <= searchDto.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.ContentType))
        {
            filteredFiles = filteredFiles.Where(f => f.ContentType.Contains(searchDto.ContentType));
        }

        if (searchDto.MinFileSize.HasValue)
        {
            filteredFiles = filteredFiles.Where(f => f.FileSize >= searchDto.MinFileSize.Value);
        }

        if (searchDto.MaxFileSize.HasValue)
        {
            filteredFiles = filteredFiles.Where(f => f.FileSize <= searchDto.MaxFileSize.Value);
        }

        // Apply sorting
        filteredFiles = searchDto.SortBy?.ToLower() switch
        {
            "filename" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredFiles.OrderBy(f => f.FileName) 
                : filteredFiles.OrderByDescending(f => f.FileName),
            "originalfilename" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredFiles.OrderBy(f => f.OriginalFileName) 
                : filteredFiles.OrderByDescending(f => f.OriginalFileName),
            "filesize" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredFiles.OrderBy(f => f.FileSize) 
                : filteredFiles.OrderByDescending(f => f.FileSize),
            "downloadcount" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredFiles.OrderBy(f => f.DownloadCount) 
                : filteredFiles.OrderByDescending(f => f.DownloadCount),
            "category" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredFiles.OrderBy(f => f.Category) 
                : filteredFiles.OrderByDescending(f => f.Category),
            _ => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredFiles.OrderBy(f => f.CreatedAt) 
                : filteredFiles.OrderByDescending(f => f.CreatedAt)
        };

        var totalCount = filteredFiles.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize);

        var pagedFiles = filteredFiles
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();

        return new PagedDownloadableFileResultDto
        {
            Files = pagedFiles,
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = searchDto.Page > 1,
            HasNextPage = searchDto.Page < totalPages
        };
    }

    public async Task<DownloadableFileDto?> UpdateFileAsync(Guid id, UpdateDownloadableFileDto updateDto, Guid userId, CancellationToken cancellationToken = default)
    {
        var file = await _unitOfWork.Repository<DownloadableFile>().GetByIdAsync(id, cancellationToken);
        if (file == null) return null;

        if (!string.IsNullOrWhiteSpace(updateDto.CustomFileName))
            file.OriginalFileName = updateDto.CustomFileName.Trim();

        if (!string.IsNullOrWhiteSpace(updateDto.Description))
            file.Description = updateDto.Description;

        if (!string.IsNullOrWhiteSpace(updateDto.Category))
            file.Category = updateDto.Category;

        if (updateDto.IsActive.HasValue)
            file.IsActive = updateDto.IsActive.Value;

        file.UpdatedBy = userId;
        file.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<DownloadableFile>().Update(file);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"File updated: {file.OriginalFileName} by user {userId}");

        return await GetFileByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _unitOfWork.Repository<DownloadableFile>().GetByIdAsync(id, cancellationToken);
        if (file == null) return false;

        // Delete physical file
        await _fileUploadService.DeleteFileAsync(file.FilePath);

        // Delete from database
        _unitOfWork.Repository<DownloadableFile>().Remove(file);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"File deleted: {file.OriginalFileName}");

        return true;
    }

    public async Task<bool> ToggleFileStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _unitOfWork.Repository<DownloadableFile>().GetByIdAsync(id, cancellationToken);
        if (file == null) return false;

        file.IsActive = !file.IsActive;
        file.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<DownloadableFile>().Update(file);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"File status toggled: {file.OriginalFileName} - Active: {file.IsActive}");

        return true;
    }

    public async Task<FileDownloadResponseDto?> DownloadFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _unitOfWork.Repository<DownloadableFile>().GetByIdAsync(id, cancellationToken);
        if (file == null || !file.IsActive) return null;

        try
        {
            // Get physical file path
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var physicalPath = Path.Combine(webRootPath, file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(physicalPath))
            {
                _logger.LogWarning($"Physical file not found: {physicalPath}");
                return null;
            }

            // Read file content
            var fileContent = await File.ReadAllBytesAsync(physicalPath, cancellationToken);

            // Increment download count
            file.DownloadCount++;
            _unitOfWork.Repository<DownloadableFile>().Update(file);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"File downloaded: {file.OriginalFileName} - Total downloads: {file.DownloadCount}");

            return new FileDownloadResponseDto
            {
                FileName = file.OriginalFileName,
                ContentType = file.ContentType,
                FileContent = fileContent,
                FileSize = file.FileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading file: {file.OriginalFileName}");
            throw;
        }
    }

    public async Task<DownloadableFileStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allFiles = await GetAllFilesAsync(cancellationToken);
        var filesList = allFiles.ToList();

        var statistics = new DownloadableFileStatisticsDto
        {
            TotalFiles = filesList.Count,
            ActiveFiles = filesList.Count(f => f.IsActive),
            InactiveFiles = filesList.Count(f => !f.IsActive),
            TotalFileSize = filesList.Sum(f => f.FileSize),
            TotalDownloads = filesList.Sum(f => f.DownloadCount),
            FilesByCategory = filesList
                .Where(f => !string.IsNullOrEmpty(f.Category))
                .GroupBy(f => f.Category!)
                .ToDictionary(g => g.Key, g => g.Count()),
            FilesByContentType = filesList
                .GroupBy(f => f.ContentType)
                .ToDictionary(g => g.Key, g => g.Count()),
            MostDownloadedFiles = filesList
                .OrderByDescending(f => f.DownloadCount)
                .Take(10)
                .ToList(),
            RecentFiles = filesList
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToList()
        };

        statistics.TotalFileSizeFormatted = FormatFileSize(statistics.TotalFileSize);

        return statistics;
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var files = await _unitOfWork.Repository<DownloadableFile>().FindAsync(f => !string.IsNullOrEmpty(f.Category), cancellationToken);

        return files
            .Select(f => f.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public async Task<IEnumerable<DownloadableFileDto>> GetFilesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var files = await _unitOfWork.Repository<DownloadableFile>().FindAsync(f => f.IsActive && f.Category == category, cancellationToken);
        var filesWithUsers = new List<DownloadableFileDto>();

        foreach (var file in files)
        {
            var fileWithUsers = await _unitOfWork.Repository<DownloadableFile>().GetByIdWithIncludesAsync(file.Id, 
                f => f.CreatedByUser, f => f.UpdatedByUser);
            if (fileWithUsers != null)
                filesWithUsers.Add(MapToDto(fileWithUsers));
        }

        return filesWithUsers.OrderByDescending(f => f.CreatedAt);
    }

    private DownloadableFileDto MapToDto(DownloadableFile file)
    {
        var dto = _mapper.Map<DownloadableFileDto>(file);
        dto.CreatedByUserName = file.CreatedByUser?.FirstName + " " + file.CreatedByUser?.LastName ?? "Unknown";
        dto.UpdatedByUserName = file.UpdatedByUser?.FirstName + " " + file.UpdatedByUser?.LastName;
        dto.DownloadUrl = $"/api/v1/files/download/{file.Id}";
        dto.FileSizeFormatted = FormatFileSize(file.FileSize);
        return dto;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}