using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

/// <summary>
/// Admin-only Net Profit Analytics controller.
/// Completely separate from StatisticsController / existing revenue dashboard.
/// Route prefix: api/v1/admin/profit
/// </summary>
[ApiController]
[Route("api/v1/admin/profit")]
[Authorize(Roles = "Admin")]
public class ProfitController : ControllerBase
{
    private readonly IProfitService _profitService;

    public ProfitController(IProfitService profitService)
    {
        _profitService = profitService;
    }

    /// <summary>
    /// GET api/v1/admin/profit/summary
    /// Returns net profit for today, this month, this year, and all-time.
    /// Only counts delivered orders.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ProfitSummaryDto), 200)]
    public async Task<ActionResult<ProfitSummaryDto>> GetProfitSummary(CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _profitService.GetProfitSummaryAsync(cancellationToken);
            return Ok(summary);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while calculating profit summary." });
        }
    }

    /// <summary>
    /// GET api/v1/admin/profit?startDate=2025-01-01&amp;endDate=2025-12-31
    /// Returns net profit analytics for the specified date range.
    /// Dates matched against order DeliveredAt timestamp.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfitByDateRangeDto), 200)]
    public async Task<ActionResult<ProfitByDateRangeDto>> GetProfitByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return BadRequest(new { message = "startDate must be before or equal to endDate." });
        }

        try
        {
            var result = await _profitService.GetProfitByDateRangeAsync(startDate, endDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while calculating profit for the specified date range." });
        }
    }

    /// <summary>
    /// GET api/v1/admin/profit/orders
    /// Returns a detailed per-order profit breakdown for all delivered orders.
    /// Includes per-item cost / selling price and net profit for each order.
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(IEnumerable<ProfitOrderDto>), 200)]
    public async Task<ActionResult<IEnumerable<ProfitOrderDto>>> GetProfitPerOrder(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _profitService.GetProfitPerOrderAsync(cancellationToken);
            return Ok(orders);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving order profit details." });
        }
    }

    /// <summary>
    /// PATCH api/v1/admin/profit/orders/{orderId}/taxi-cost
    /// Updates the delivery/taxi cost for an order.
    /// Admin entry that affects net profit calculations.
    /// </summary>
    [HttpPatch("orders/{orderId}/taxi-cost")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> UpdateOrderTaxiCost(
        Guid orderId,
        [FromBody] UpdateOrderTaxiCostDto updateDto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _profitService.UpdateOrderTaxiCostAsync(orderId, updateDto.TaxiCost, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while updating taxi cost." });
        }
    }
}
