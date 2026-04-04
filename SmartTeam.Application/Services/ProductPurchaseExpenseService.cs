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
    // Update
    // ─────────────────────────────────────────────────────────────────

    public async Task<ProductPurchaseExpenseDto> UpdateAsync(
        Guid id,
        UpdateProductPurchaseExpenseDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch old record
        var expense = await _unitOfWork.Repository<ProductPurchaseExpense>()
            .GetByIdAsync(id, cancellationToken);
        
        if (expense == null)
            throw new ArgumentException("Expense record not found.");

        // 2. Validate inputs
        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (dto.UnitPurchasePrice <= 0)
            throw new ArgumentException("Unit purchase price must be greater than zero.");

        // 3. Handle Stock Adjustment part 1: Revert old product stock
        var oldProduct = await _unitOfWork.Repository<Product>().GetByIdAsync(expense.ProductId, cancellationToken);
        if (oldProduct != null)
        {
            oldProduct.StockQuantity -= expense.Quantity; // Subtract old quantity
            oldProduct.UpdatedAt      = TimeHelper.Now;
            _unitOfWork.Repository<Product>().Update(oldProduct);
        }

        // 4. Handle Product Change & Snapshot
        if (expense.ProductId != dto.ProductId)
        {
            var newProduct = await _unitOfWork.Repository<Product>().GetByIdAsync(dto.ProductId, cancellationToken);
            if (newProduct == null)
                throw new ArgumentException($"New product with ID '{dto.ProductId}' not found.");
            
            expense.ProductId   = dto.ProductId;
            expense.ProductName = newProduct.Name; // Update snapshot
        }

        // 5. Update Stock Adjustment part 2: Apply new quantity to target product
        var targetProduct = await _unitOfWork.Repository<Product>().GetByIdAsync(dto.ProductId, cancellationToken);
        if (targetProduct == null)
            throw new ArgumentException("Target product not found.");

        targetProduct.StockQuantity += dto.Quantity; // Add new quantity
        targetProduct.UpdatedAt      = TimeHelper.Now;
        _unitOfWork.Repository<Product>().Update(targetProduct);

        // 6. Regenerate Invoice Number if date changed
        if (expense.PurchaseDate.Date != dto.PurchaseDate.Date)
        {
            expense.InvoiceNumber = await GenerateInvoiceNumberAsync(dto.PurchaseDate, cancellationToken);
        }

        // 7. Update remaining fields
        expense.Quantity          = dto.Quantity;
        expense.UnitPurchasePrice = dto.UnitPurchasePrice;
        expense.TotalExpense      = dto.Quantity * dto.UnitPurchasePrice;
        expense.PurchaseDate      = dto.PurchaseDate;
        expense.SupplierName      = dto.SupplierName;
        expense.Notes             = dto.Notes;

        _unitOfWork.Repository<ProductPurchaseExpense>().Update(expense);
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

        // Revert stock on deletion
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(expense.ProductId, cancellationToken);
        if (product != null)
        {
            product.StockQuantity -= expense.Quantity;
            product.UpdatedAt      = TimeHelper.Now;
            _unitOfWork.Repository<Product>().Update(product);
        }

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

        // Fetch existing entries for the same day to find the maximum sequence used
        var existingOnDay = await _unitOfWork.Repository<ProductPurchaseExpense>()
            .FindAsync(e => e.InvoiceNumber.StartsWith(prefix), cancellationToken);

        int maxSequence = 0;
        foreach (var expense in existingOnDay)
        {
            // Format is PEXP-YYYYMMDD-XXXX
            var parts = expense.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int seq))
            {
                if (seq > maxSequence) maxSequence = seq;
            }
        }

        var nextSequence = maxSequence + 1;
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
