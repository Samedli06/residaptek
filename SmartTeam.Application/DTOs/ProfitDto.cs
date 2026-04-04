namespace SmartTeam.Application.DTOs;

/// <summary>
/// Summary of net profit across time buckets — admin only.
/// </summary>
public class ProfitSummaryDto
{
    public decimal TodayNetProfit { get; set; }
    public decimal MonthlyNetProfit { get; set; }
    public decimal YearlyNetProfit { get; set; }
    public decimal TotalNetProfit { get; set; }

    // Supporting totals for context
    public decimal TodayRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal YearlyRevenue { get; set; }
    public decimal TotalRevenue { get; set; }

    public decimal TodayCost { get; set; }
    public decimal MonthlyCost { get; set; }
    public decimal YearlyCost { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Net profit analytics for a custom date range — admin only.
/// </summary>
public class ProfitByDateRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalNetProfit { get; set; }

    /// <summary>
    /// Average net profit per delivered order in the range.
    /// </summary>
    public decimal AverageNetProfitPerOrder { get; set; }
}

/// <summary>
/// Per-order profit breakdown — admin only.
/// </summary>
public class ProfitOrderDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? DeliveredAt { get; set; }

    /// <summary>Total selling price of all items (before discounts).</summary>
    public decimal OrderSubTotal { get; set; }

    /// <summary>Total amount actually paid (after all discounts).</summary>
    public decimal OrderTotalAmount { get; set; }

    /// <summary>Sum of (UnitCostPrice × Quantity) across all items.</summary>
    public decimal OrderCostTotal { get; set; }

    /// <summary>Net profit = OrderTotalAmount - OrderCostTotal.</summary>
    public decimal OrderNetProfit { get; set; }

    /// <summary>Wallet discount applied (reduces effective revenue).</summary>
    public decimal? WalletDiscount { get; set; }

    /// <summary>Promo code discount applied (reduces effective revenue).</summary>
    public decimal? PromoDiscount { get; set; }

    /// <summary>Delivery/taxi cost entered by admin.</summary>
    public decimal? TaxiCost { get; set; }

    public List<ProfitOrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderTaxiCostDto
{
    public decimal TaxiCost { get; set; }
}

/// <summary>
/// Per-item profit breakdown within an order.
/// </summary>
public class ProfitOrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }

    /// <summary>Selling price per unit at time of order.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Cost price per unit at time of order (snapshotted).</summary>
    public decimal? UnitCostPrice { get; set; }

    /// <summary>Net profit for this line item = (UnitPrice - UnitCostPrice) × Quantity.</summary>
    public decimal NetProfitPerItem { get; set; }
}
