using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class PromoCodesController : ControllerBase
{
    private readonly IPromoCodeService _promoCodeService;

    public PromoCodesController(IPromoCodeService promoCodeService)
    {
        _promoCodeService = promoCodeService;
    }

    /// <summary>
    /// Create a new promo code (Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PromoCodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromoCodeDto>> CreatePromoCode([FromBody] CreatePromoCodeDto createDto, CancellationToken cancellationToken)
    {
        try
        {
            var promoCode = await _promoCodeService.CreatePromoCodeAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetPromoCodeById), new { id = promoCode.Id }, promoCode);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all promo codes with pagination (Admin only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<PromoCodeListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<PromoCodeListDto>>> GetAllPromoCodes(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _promoCodeService.GetAllPromoCodesAsync(pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get promo code by ID (Admin only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PromoCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromoCodeDto>> GetPromoCodeById(Guid id, CancellationToken cancellationToken)
    {
        var promoCode = await _promoCodeService.GetPromoCodeByIdAsync(id, cancellationToken);
        
        if (promoCode == null)
        {
            return NotFound(new { error = "Promo code not found." });
        }

        return Ok(promoCode);
    }

    /// <summary>
    /// Update promo code (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PromoCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromoCodeDto>> UpdatePromoCode(Guid id, [FromBody] UpdatePromoCodeDto updateDto, CancellationToken cancellationToken)
    {
        try
        {
            var promoCode = await _promoCodeService.UpdatePromoCodeAsync(id, updateDto, cancellationToken);
            return Ok(promoCode);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete promo code (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePromoCode(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _promoCodeService.DeletePromoCodeAsync(id, cancellationToken);
            return Ok(new { message = "Promo code deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get promo code usage history (Admin only)
    /// </summary>
    [HttpGet("{id:guid}/usage")]
    [ProducesResponseType(typeof(PagedResultDto<PromoCodeUsageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<PromoCodeUsageDto>>> GetPromoCodeUsageHistory(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Verify promo code exists
        var promoCode = await _promoCodeService.GetPromoCodeByIdAsync(id, cancellationToken);
        if (promoCode == null)
        {
            return NotFound(new { error = "Promo code not found." });
        }

        var result = await _promoCodeService.GetPromoCodeUsageHistoryAsync(id, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Validate promo code (Admin only - for testing)
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(PromoCodeValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PromoCodeValidationResultDto>> ValidatePromoCode([FromBody] ApplyPromoCodeDto dto, CancellationToken cancellationToken)
    {
        var result = await _promoCodeService.ValidatePromoCodeAsync(dto.PromoCode, cancellationToken);
        return Ok(result);
    }
}
