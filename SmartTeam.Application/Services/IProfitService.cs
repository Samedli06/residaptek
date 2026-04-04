using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

/// <summary>
/// Admin-only service for calculating net profit analytics.
/// Completely separate from the existing revenue/statistics module.
/// </summary>
public interface IProfitService
{
    /// <summary>
    /// Returns net profit buckets: today, this month, this year, all-time.
    /// Only delivered orders are counted.
    /// </summary>
    Task<ProfitSummaryDto> GetProfitSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns net profit analytics for a custom date range.
    /// Dates are matched against order DeliveredAt timestamp.
    /// </summary>
    Task<ProfitByDateRangeDto> GetProfitByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a detailed per-order profit breakdown for all delivered orders.
    /// Includes per-item cost/selling price and net profit.
    /// </summary>
    Task<IEnumerable<ProfitOrderDto>> GetProfitPerOrderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the delivery/taxi cost for an order.
    /// Admin entry that affects net profit calculations.
    /// </summary>
    Task UpdateOrderTaxiCostAsync(Guid orderId, decimal taxiCost, CancellationToken cancellationToken = default);
}
