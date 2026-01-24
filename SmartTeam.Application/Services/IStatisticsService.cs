using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IStatisticsService
{
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default);
}
