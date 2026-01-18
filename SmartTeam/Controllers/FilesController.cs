using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly IDownloadableFileService _downloadableFileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IDownloadableFileService downloadableFileService, ILogger<FilesController> logger)
    {
        _downloadableFileService = downloadableFileService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active downloadable files (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DownloadableFileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DownloadableFileDto>>> GetActiveFiles(CancellationToken cancellationToken)
    {
        var files = await _downloadableFileService.GetActiveFilesAsync(cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Get files by category (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<DownloadableFileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DownloadableFileDto>>> GetFilesByCategory(string category, CancellationToken cancellationToken)
    {
        var files = await _downloadableFileService.GetFilesByCategoryAsync(category, cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Get all available categories (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _downloadableFileService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Get file information by ID (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DownloadableFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DownloadableFileDto>> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var file = await _downloadableFileService.GetFileByIdAsync(id, cancellationToken);
        if (file == null || !file.IsActive)
        {
            return NotFound();
        }

        return Ok(file);
    }

    /// <summary>
    /// Download file by ID (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("download/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var fileResponse = await _downloadableFileService.DownloadFileAsync(id, cancellationToken);
            if (fileResponse == null)
            {
                return NotFound("File not found or not available for download");
            }

            _logger.LogInformation($"File download initiated: {fileResponse.FileName}");

            return File(
                fileResponse.FileContent,
                fileResponse.ContentType,
                fileResponse.FileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading file with ID: {id}");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while downloading the file");
        }
    }

    /// <summary>
    /// Search files with basic filtering (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedDownloadableFileResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedDownloadableFileResultDto>> SearchFiles(
        [FromQuery] string? searchTerm,
        [FromQuery] string? category,
        [FromQuery] string? contentType,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchDto = new DownloadableFileSearchDto
            {
                SearchTerm = searchTerm,
                Category = category,
                ContentType = contentType,
                IsActive = true, // Only show active files for public access
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = Math.Min(pageSize, 50) // Limit page size for public access
            };

            var result = await _downloadableFileService.SearchFilesAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching files");
            return BadRequest("An error occurred while searching files");
        }
    }
}
