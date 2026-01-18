using AutoMapper;
using Microsoft.Extensions.Logging;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class ProductPdfService : IProductPdfService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileUploadService _fileUploadService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductPdfService> _logger;

    public ProductPdfService(
        IUnitOfWork unitOfWork,
        IFileUploadService fileUploadService,
        IMapper mapper,
        ILogger<ProductPdfService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileUploadService = fileUploadService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductPdfDto> UploadPdfAsync(CreateProductPdfDto createDto, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate product exists
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(createDto.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException("Product not found");

            // Validate file is PDF
            if (createDto.PdfFile.ContentType != "application/pdf")
                throw new ArgumentException("Only PDF files are allowed");

            // Check if product already has a PDF (since we have unique constraint)
            var existingPdf = await _unitOfWork.Repository<ProductPdf>()
                .FirstOrDefaultAsync(p => p.ProductId == createDto.ProductId, cancellationToken);
            
            if (existingPdf != null)
                throw new InvalidOperationException("Product already has a PDF file. Please delete the existing one first or update it.");

            // Upload the PDF file
            var filePath = await _fileUploadService.UploadFileAsync(createDto.PdfFile, "product-pdfs");
            
            // Determine the display filename
            var displayFileName = !string.IsNullOrWhiteSpace(createDto.CustomFileName) 
                ? createDto.CustomFileName.Trim()
                : createDto.PdfFile.FileName;

            // Ensure filename has .pdf extension
            if (!displayFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                displayFileName += ".pdf";
            
            // Create the entity
            var productPdf = new ProductPdf
            {
                Id = Guid.NewGuid(),
                ProductId = createDto.ProductId,
                FileName = Path.GetFileName(filePath), // Physical filename on disk
                OriginalFileName = displayFileName, // Display name for users
                FilePath = filePath,
                ContentType = createDto.PdfFile.ContentType,
                FileSize = createDto.PdfFile.Length,
                Description = createDto.Description,
                IsActive = createDto.IsActive,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<ProductPdf>().AddAsync(productPdf, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"PDF uploaded successfully for product {product.Name}: {productPdf.OriginalFileName} by user {userId}");

            return await GetPdfByIdAsync(productPdf.Id, cancellationToken) ?? throw new InvalidOperationException("Failed to retrieve uploaded PDF");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading PDF for product {createDto.ProductId}: {createDto.PdfFile.FileName}");
            throw;
        }
    }

    public async Task<ProductPdfDto?> GetPdfByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var pdf = await _unitOfWork.Repository<ProductPdf>().GetByIdWithIncludesAsync(id, 
            p => p.Product, p => p.CreatedByUser, p => p.UpdatedByUser);

        if (pdf == null) return null;

        return MapToDto(pdf);
    }

    public async Task<ProductPdfDto?> GetPdfByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var pdf = await _unitOfWork.Repository<ProductPdf>()
            .FirstOrDefaultWithIncludesAsync(p => p.ProductId == productId, 
                p => p.Product, p => p.CreatedByUser, p => p.UpdatedByUser);

        if (pdf == null) return null;

        return MapToDto(pdf);
    }

    public async Task<IEnumerable<ProductPdfDto>> GetAllPdfsAsync(CancellationToken cancellationToken = default)
    {
        var pdfs = await _unitOfWork.Repository<ProductPdf>().GetAllWithIncludesAsync(
            p => p.Product, p => p.CreatedByUser, p => p.UpdatedByUser);

        return pdfs.Select(MapToDto).OrderByDescending(p => p.CreatedAt);
    }

    public async Task<IEnumerable<ProductPdfDto>> GetActivePdfsAsync(CancellationToken cancellationToken = default)
    {
        var pdfs = await _unitOfWork.Repository<ProductPdf>().FindAsync(p => p.IsActive, cancellationToken);
        var pdfsWithIncludes = new List<ProductPdfDto>();

        foreach (var pdf in pdfs)
        {
            var pdfWithIncludes = await _unitOfWork.Repository<ProductPdf>().GetByIdWithIncludesAsync(pdf.Id, 
                p => p.Product, p => p.CreatedByUser, p => p.UpdatedByUser);
            if (pdfWithIncludes != null)
                pdfsWithIncludes.Add(MapToDto(pdfWithIncludes));
        }

        return pdfsWithIncludes.OrderByDescending(p => p.CreatedAt);
    }

    public async Task<PagedProductPdfResultDto> SearchPdfsAsync(ProductPdfSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        // For simplicity, get all PDFs and filter in memory
        var allPdfs = await GetAllPdfsAsync(cancellationToken);
        var filteredPdfs = allPdfs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLower();
            filteredPdfs = filteredPdfs.Where(p => 
                p.OriginalFileName.ToLower().Contains(searchTerm) ||
                p.ProductName.ToLower().Contains(searchTerm) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
        }

        if (searchDto.ProductId.HasValue)
        {
            filteredPdfs = filteredPdfs.Where(p => p.ProductId == searchDto.ProductId.Value);
        }

        if (searchDto.IsActive.HasValue)
        {
            filteredPdfs = filteredPdfs.Where(p => p.IsActive == searchDto.IsActive.Value);
        }

        if (searchDto.CreatedFrom.HasValue)
        {
            filteredPdfs = filteredPdfs.Where(p => p.CreatedAt >= searchDto.CreatedFrom.Value);
        }

        if (searchDto.CreatedTo.HasValue)
        {
            filteredPdfs = filteredPdfs.Where(p => p.CreatedAt <= searchDto.CreatedTo.Value);
        }

        if (searchDto.MinFileSize.HasValue)
        {
            filteredPdfs = filteredPdfs.Where(p => p.FileSize >= searchDto.MinFileSize.Value);
        }

        if (searchDto.MaxFileSize.HasValue)
        {
            filteredPdfs = filteredPdfs.Where(p => p.FileSize <= searchDto.MaxFileSize.Value);
        }

        // Apply sorting
        filteredPdfs = searchDto.SortBy?.ToLower() switch
        {
            "filename" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredPdfs.OrderBy(p => p.FileName) 
                : filteredPdfs.OrderByDescending(p => p.FileName),
            "originalfilename" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredPdfs.OrderBy(p => p.OriginalFileName) 
                : filteredPdfs.OrderByDescending(p => p.OriginalFileName),
            "productname" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredPdfs.OrderBy(p => p.ProductName) 
                : filteredPdfs.OrderByDescending(p => p.ProductName),
            "filesize" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredPdfs.OrderBy(p => p.FileSize) 
                : filteredPdfs.OrderByDescending(p => p.FileSize),
            "downloadcount" => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredPdfs.OrderBy(p => p.DownloadCount) 
                : filteredPdfs.OrderByDescending(p => p.DownloadCount),
            _ => searchDto.SortOrder?.ToLower() == "asc" 
                ? filteredPdfs.OrderBy(p => p.CreatedAt) 
                : filteredPdfs.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = filteredPdfs.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize);

        var pagedPdfs = filteredPdfs
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();

        return new PagedProductPdfResultDto
        {
            Files = pagedPdfs,
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = searchDto.Page > 1,
            HasNextPage = searchDto.Page < totalPages
        };
    }

    public async Task<ProductPdfDto?> UpdatePdfAsync(Guid id, UpdateProductPdfDto updateDto, Guid userId, CancellationToken cancellationToken = default)
    {
        var pdf = await _unitOfWork.Repository<ProductPdf>().GetByIdAsync(id, cancellationToken);
        if (pdf == null) return null;

        if (!string.IsNullOrWhiteSpace(updateDto.CustomFileName))
        {
            var displayFileName = updateDto.CustomFileName.Trim();
            // Ensure filename has .pdf extension
            if (!displayFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                displayFileName += ".pdf";
            pdf.OriginalFileName = displayFileName;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Description))
            pdf.Description = updateDto.Description;

        if (updateDto.IsActive.HasValue)
            pdf.IsActive = updateDto.IsActive.Value;

        pdf.UpdatedBy = userId;
        pdf.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<ProductPdf>().Update(pdf);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"PDF updated: {pdf.OriginalFileName} by user {userId}");

        return await GetPdfByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeletePdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var pdf = await _unitOfWork.Repository<ProductPdf>().GetByIdAsync(id, cancellationToken);
        if (pdf == null) return false;

        // Delete physical file
        await _fileUploadService.DeleteFileAsync(pdf.FilePath);

        // Delete from database
        _unitOfWork.Repository<ProductPdf>().Remove(pdf);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"PDF deleted: {pdf.OriginalFileName}");

        return true;
    }

    public async Task<bool> TogglePdfStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var pdf = await _unitOfWork.Repository<ProductPdf>().GetByIdAsync(id, cancellationToken);
        if (pdf == null) return false;

        pdf.IsActive = !pdf.IsActive;
        pdf.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<ProductPdf>().Update(pdf);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"PDF status toggled: {pdf.OriginalFileName} - Active: {pdf.IsActive}");

        return true;
    }

    public async Task<ProductPdfDownloadResponseDto?> DownloadPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var pdf = await _unitOfWork.Repository<ProductPdf>().GetByIdAsync(id, cancellationToken);
        if (pdf == null || !pdf.IsActive) return null;

        return await ProcessDownload(pdf, cancellationToken);
    }

    public async Task<ProductPdfDownloadResponseDto?> DownloadPdfByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var pdf = await _unitOfWork.Repository<ProductPdf>()
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive, cancellationToken);
        
        if (pdf == null) return null;

        return await ProcessDownload(pdf, cancellationToken);
    }

    public async Task<bool> HasPdfAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<ProductPdf>()
            .AnyAsync(p => p.ProductId == productId && p.IsActive, cancellationToken);
    }

    private async Task<ProductPdfDownloadResponseDto?> ProcessDownload(ProductPdf pdf, CancellationToken cancellationToken)
    {
        try
        {
            // Get physical file path
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var physicalPath = Path.Combine(webRootPath, pdf.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(physicalPath))
            {
                _logger.LogWarning($"Physical PDF file not found: {physicalPath}");
                return null;
            }

            // Read file content
            var fileContent = await File.ReadAllBytesAsync(physicalPath, cancellationToken);

            // Increment download count
            pdf.DownloadCount++;
            _unitOfWork.Repository<ProductPdf>().Update(pdf);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"PDF downloaded: {pdf.OriginalFileName} - Total downloads: {pdf.DownloadCount}");

            return new ProductPdfDownloadResponseDto
            {
                FileName = pdf.OriginalFileName,
                ContentType = pdf.ContentType,
                FileContent = fileContent,
                FileSize = pdf.FileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading PDF: {pdf.OriginalFileName}");
            throw;
        }
    }

    private ProductPdfDto MapToDto(ProductPdf pdf)
    {
        var dto = _mapper.Map<ProductPdfDto>(pdf);
        dto.ProductName = pdf.Product?.Name ?? "Unknown Product";
        dto.CreatedByUserName = pdf.CreatedByUser?.FirstName + " " + pdf.CreatedByUser?.LastName ?? "Unknown";
        dto.UpdatedByUserName = pdf.UpdatedByUser?.FirstName + " " + pdf.UpdatedByUser?.LastName;
        dto.DownloadUrl = $"/api/v1/product-pdfs/download/{pdf.Id}";
        dto.FileSizeFormatted = FormatFileSize(pdf.FileSize);
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
