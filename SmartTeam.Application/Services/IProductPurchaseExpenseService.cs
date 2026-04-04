using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

/// <summary>
/// Admin-only service for recording and querying product purchase expenses.
/// Completely isolated from sales, profit, and statistics modules.
/// </summary>
public interface IProductPurchaseExpenseService
{
    /// <summary>
    /// Creates a new purchase expense entry.
    /// Validates product exists, snapshots product name, calculates and stores TotalExpense.
    /// </summary>
    Task<ProductPurchaseExpenseDto> CreateAsync(
        CreateProductPurchaseExpenseDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing purchase expense entry.
    /// Manages stock updates for both old and new products.
    /// </summary>
    Task<ProductPurchaseExpenseDto> UpdateAsync(
        Guid id,
        UpdateProductPurchaseExpenseDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all expense entries ordered by PurchaseDate descending.</summary>
    Task<IEnumerable<ProductPurchaseExpenseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single expense entry by ID (invoice view).</summary>
    Task<ProductPurchaseExpenseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregated expense totals for today, this month, this year, and all-time.
    /// Based on PurchaseDate (not CreatedAt).
    /// </summary>
    Task<ProductPurchaseExpenseSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns expense totals for a custom date range.
    /// Matched against PurchaseDate.
    /// </summary>
    Task<ProductPurchaseExpenseByDateRangeDto> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>Permanently deletes an expense entry. Returns false if not found.</summary>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
