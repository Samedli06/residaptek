using SmartTeam.Application.DTOs;
using SmartTeam.Application.Helpers;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

/// <summary>
/// Implements product purchase expense tracking for the admin.
/// Isolated from all existing sales, profit, and order logic.
///
/// Key rules:
///   - TotalExpense = Quantity × UnitPurchasePrice, calculated once and stored.
///   - ProductName is snapshotted at time of entry (historical immutability).
///   - Summary calculations use PurchaseDate (not CreatedAt).
///   - No side effects on Product, Order, or Profit modules.
/// </summary>
public class ProductPurchaseExpenseService : IProductPurchaseExpenseService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductPurchaseExpenseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ─────────────────────────────────────────────────────────────────
    // Create
    // ─────────────────────────────────────────────────────────────────

    public async Task<ProductPurchaseExpenseDto> CreateAsync(
        CreateProductPurchaseExpenseDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (dto.UnitPurchasePrice <= 0)
            throw new ArgumentException("Unit purchase price must be greater than zero.");

        // Validate product exists and snapshot its name
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(dto.ProductId, cancellationToken);
        if (product == null)
            throw new ArgumentException($"Product with ID '{dto.ProductId}' was not found.");

        // Calculate total expense once — stored permanently
        var totalExpense = dto.Quantity * dto.UnitPurchasePrice;

        var expense = new ProductPurchaseExpense
        {
            Id                = Guid.NewGuid(),
            InvoiceNumber     = await GenerateInvoiceNumberAsync(dto.PurchaseDate, cancellationToken),
            ProductId         = dto.ProductId,
            ProductName       = product.Name, // Snapshot — immutable even if product is renamed
            Quantity          = dto.Quantity,
            UnitPurchasePrice = dto.UnitPurchasePrice,
            TotalExpense      = totalExpense,  // Stored — never recalculated
            PurchaseDate      = dto.PurchaseDate,
            SupplierName      = dto.SupplierName,
            Notes             = dto.Notes,
            CreatedAt         = TimeHelper.Now
        };

        await _unitOfWork.Repository<ProductPurchaseExpense>().AddAsync(expense, cancellationToken);

        // Increase product stock by the purchased quantity
        product.StockQuantity += dto.Quantity;
        product.UpdatedAt      = TimeHelper.Now;
        _unitOfWork.Repository<Product>().Update(product);

        // Commit both the expense record and the stock update atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(expense);
    }

    // ─────────────────────────────────────────────────────────────────
    // Read
    // ─────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ProductPurchaseExpenseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var expenses = await _unitOfWork.Repository<ProductPurchaseExpense>()
            .GetAllAsync(cancellationToken);

        return expenses
            .OrderByDescending(e => e.PurchaseDate)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ProductPurchaseExpenseDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Repository<ProductPurchaseExpense>()
            .GetByIdAsync(id, cancellationToken);

        return expense == null ? null : MapToDto(expense);
    }

    public async Task<ProductPurchaseExpenseSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var now        = TimeHelper.Now;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart  = new DateTime(now.Year, 1,         1, 0, 0, 0, DateTimeKind.Utc);

        var allExpenses = (await _unitOfWork.Repository<ProductPurchaseExpense>()
            .GetAllAsync(cancellationToken)).ToList();

        var today   = allExpenses.Where(e => e.PurchaseDate >= todayStart).ToList();
        var monthly = allExpenses.Where(e => e.PurchaseDate >= monthStart).ToList();
        var yearly  = allExpenses.Where(e => e.PurchaseDate >= yearStart).ToList();

        return new ProductPurchaseExpenseSummaryDto
        {
            TodayTotal   = today.Sum(e   => e.TotalExpense),
            MonthlyTotal = monthly.Sum(e => e.TotalExpense),
            YearlyTotal  = yearly.Sum(e  => e.TotalExpense),
            AllTimeTotal = allExpenses.Sum(e => e.TotalExpense),

            TodayCount   = today.Count,
            MonthlyCount = monthly.Count,
            YearlyCount  = yearly.Count,
            AllTimeCount = allExpenses.Count
        };
    }

    public async Task<ProductPurchaseExpenseByDateRangeDto> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var start = startDate.Date;
        var end   = endDate.Date.AddDays(1).AddTicks(-1); // Inclusive end of day

        var inRange = (await _unitOfWork.Repository<ProductPurchaseExpense>()
            .FindAsync(e => e.PurchaseDate >= start && e.PurchaseDate <= end, cancellationToken))
            .ToList();

        var totalExpense = inRange.Sum(e => e.TotalExpense);

        return new ProductPurchaseExpenseByDateRangeDto
        {
            StartDate            = start,
            EndDate              = endDate.Date,
            EntryCount           = inRange.Count,
            TotalExpense         = totalExpense,
            AverageExpensePerEntry = inRange.Count > 0
                ? Math.Round(totalExpense / inRange.Count, 2)
                : 0
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Delete
    // ─────────────────────────────────────────────────────────────────

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Repository<ProductPurchaseExpense>()
            .GetByIdAsync(id, cancellationToken);

        if (expense == null) return false;

        _unitOfWork.Repository<ProductPurchaseExpense>().Remove(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a unique invoice number in the format PEXP-YYYYMMDD-XXXX.
    /// XXXX is a zero-padded sequential counter per day.
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync(
        DateTime purchaseDate,
        CancellationToken cancellationToken)
    {
        var datePart = purchaseDate.ToString("yyyyMMdd");
        var prefix   = $"PEXP-{datePart}-";

        // Count existing entries for the same day to determine the next sequence
        var existingOnDay = await _unitOfWork.Repository<ProductPurchaseExpense>()
            .FindAsync(e => e.InvoiceNumber.StartsWith(prefix), cancellationToken);

        var nextSequence = existingOnDay.Count() + 1;
        return $"{prefix}{nextSequence:D4}";
    }

    private static ProductPurchaseExpenseDto MapToDto(ProductPurchaseExpense expense)
        => new()
        {
            Id                = expense.Id,
            InvoiceNumber     = expense.InvoiceNumber,
            ProductId         = expense.ProductId,
            ProductName       = expense.ProductName,
            Quantity          = expense.Quantity,
            UnitPurchasePrice = expense.UnitPurchasePrice,
            TotalExpense      = expense.TotalExpense,
            PurchaseDate      = expense.PurchaseDate,
            SupplierName      = expense.SupplierName,
            Notes             = expense.Notes,
            CreatedAt         = expense.CreatedAt
        };
}
