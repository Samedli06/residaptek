using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatisticsDto>> GetDashboardStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _statisticsService.GetDashboardStatisticsAsync(cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            // Log exception
            return StatusCode(500, new { message = "An error occurred while retrieving statistics." });
        }
    }
}
