namespace SmartTeam.Domain.Entities;

/// <summary>
/// Represents a product purchase expense recorded by the admin.
/// Tracks store purchases (invoices) independently from sales, profit, and statistics modules.
/// TotalExpense is calculated once at creation and stored permanently — never recalculated.
/// </summary>
public class ProductPurchaseExpense
{
    public Guid Id { get; set; }

    /// <summary>Auto-generated invoice number, e.g. PEXP-20260402-0001</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Reference to the product purchased.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Product name snapshot at time of entry — preserved even if product is renamed/deleted.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Number of units purchased. Must be greater than 0.</summary>
    public int Quantity { get; set; }

    /// <summary>Cost per unit at time of purchase. Must be greater than 0.</summary>
    public decimal UnitPurchasePrice { get; set; }

    /// <summary>
    /// Total expense = Quantity × UnitPurchasePrice.
    /// Calculated once at creation and stored. Never dynamically recalculated.
    /// </summary>
    public decimal TotalExpense { get; set; }

    /// <summary>The date on which the purchase was made (entered by admin).</summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>Optional supplier/vendor name.</summary>
    public string? SupplierName { get; set; }

    /// <summary>Optional admin notes about this purchase.</summary>
    public string? Notes { get; set; }

    /// <summary>Record creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation property (optional — restrict delete to preserve historical records)
    public Product? Product { get; set; }
}
