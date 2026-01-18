using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/product-pdfs")]
[Produces("application/json")]
public class ProductPdfsController : ControllerBase
{
    private readonly IProductPdfService _productPdfService;
    private readonly ILogger<ProductPdfsController> _logger;

    public ProductPdfsController(IProductPdfService productPdfService, ILogger<ProductPdfsController> logger)
    {
        _productPdfService = productPdfService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active product PDFs (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductPdfDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductPdfDto>>> GetActiveProductPdfs(CancellationToken cancellationToken)
    {
        var pdfs = await _productPdfService.GetActivePdfsAsync(cancellationToken);
        return Ok(pdfs);
    }

    /// <summary>
    /// Get product PDF information by ID (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductPdfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductPdfDto>> GetProductPdf(Guid id, CancellationToken cancellationToken)
    {
        var pdf = await _productPdfService.GetPdfByIdAsync(id, cancellationToken);
        if (pdf == null || !pdf.IsActive)
        {
            return NotFound();
        }

        return Ok(pdf);
    }

    /// <summary>
    /// Get product PDF information by product ID (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(ProductPdfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductPdfDto>> GetProductPdfByProductId(Guid productId, CancellationToken cancellationToken)
    {
        var pdf = await _productPdfService.GetPdfByProductIdAsync(productId, cancellationToken);
        if (pdf == null || !pdf.IsActive)
        {
            return NotFound();
        }

        return Ok(pdf);
    }

    /// <summary>
    /// Download product PDF by ID (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("download/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DownloadProductPdf(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var pdfResponse = await _productPdfService.DownloadPdfAsync(id, cancellationToken);
            if (pdfResponse == null)
            {
                return NotFound("PDF not found or not available for download");
            }

            _logger.LogInformation($"Product PDF download initiated: {pdfResponse.FileName}");

            return File(
                pdfResponse.FileContent,
                pdfResponse.ContentType,
                pdfResponse.FileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading product PDF with ID: {id}");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while downloading the PDF");
        }
    }

    /// <summary>
    /// Download product PDF by product ID (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("download/product/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DownloadProductPdfByProductId(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            var pdfResponse = await _productPdfService.DownloadPdfByProductIdAsync(productId, cancellationToken);
            if (pdfResponse == null)
            {
                return NotFound("PDF not found or not available for download");
            }

            _logger.LogInformation($"Product PDF download initiated by product ID {productId}: {pdfResponse.FileName}");

            return File(
                pdfResponse.FileContent,
                pdfResponse.ContentType,
                pdfResponse.FileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading product PDF for product ID: {productId}");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while downloading the PDF");
        }
    }

    /// <summary>
    /// Check if product has PDF (Public access)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("product/{productId:guid}/has-pdf")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> CheckProductHasPdf(Guid productId, CancellationToken cancellationToken)
    {
        var hasPdf = await _productPdfService.HasPdfAsync(productId, cancellationToken);
        return Ok(new { productId, hasPdf });
    }
}
