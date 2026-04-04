namespace SmartTeam.Application.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// Input DTOs (Admin → API)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Admin input to create a new product purchase expense entry.
/// TotalExpense is calculated automatically by the service.
/// </summary>
public class CreateProductPurchaseExpenseDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Admin input to update an existing product purchase expense entry.
/// </summary>
public class UpdateProductPurchaseExpenseDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Response DTOs (API → Client)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Full expense entry response — used for listing and single-record (invoice) view.
/// </summary>
public class ProductPurchaseExpenseDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }

    /// <summary>Pre-calculated and stored at creation: Quantity × UnitPurchasePrice.</summary>
    public decimal TotalExpense { get; set; }

    public DateTime PurchaseDate { get; set; }
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Aggregated expense totals across time buckets — admin-only summary.
/// </summary>
public class ProductPurchaseExpenseSummaryDto
{
    // Total expense amounts
    public decimal TodayTotal { get; set; }
    public decimal MonthlyTotal { get; set; }
    public decimal YearlyTotal { get; set; }
    public decimal AllTimeTotal { get; set; }

    // Entry counts
    public int TodayCount { get; set; }
    public int MonthlyCount { get; set; }
    public int YearlyCount { get; set; }
    public int AllTimeCount { get; set; }
}

/// <summary>
/// Expense analytics for a custom date range.
/// </summary>
public class ProductPurchaseExpenseByDateRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int EntryCount { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal AverageExpensePerEntry { get; set; }
}
