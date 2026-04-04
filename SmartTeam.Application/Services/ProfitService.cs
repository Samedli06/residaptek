using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using SmartTeam.Application.Helpers;

namespace SmartTeam.Application.Services;

/// <summary>
/// Calculates net profit (real earnings) analytics for admin use.
/// Completely separate from the existing StatisticsService / revenue module.
///
/// Profit rules:
///   - Only Delivered orders are included.
///   - Per-item cost: uses UnitCostPrice (snapshotted at order time).
///     If UnitCostPrice is null (legacy order before feature), falls back to current Product.PurchasePrice.
///     If neither is set, cost is treated as 0.
///   - Wallet discount is subtracted at the ORDER level from gross item profit.
///   - Promo discount is subtracted at the ORDER level (we calculate from UnitPrice, so promo is real lost revenue).
///   - Taxi costs (delivery fees) are subtracted at the ORDER level from gross item profit.
///   - Bonus amounts are NOT counted as profit (they are a liability).
/// </summary>
public class ProfitService : IProfitService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProfitService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ─────────────────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────────────────

    public async Task<ProfitSummaryDto> GetProfitSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = TimeHelper.Now;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart  = new DateTime(now.Year, 1,         1, 0, 0, 0, DateTimeKind.Utc);

        var (allOrders, productCostFallback) = await GetDeliveredOrdersWithItemsAsync(cancellationToken);

        var today   = allOrders.Where(o => o.Order.DeliveredAt >= todayStart).ToList();
        var monthly = allOrders.Where(o => o.Order.DeliveredAt >= monthStart).ToList();
        var yearly  = allOrders.Where(o => o.Order.DeliveredAt >= yearStart).ToList();
        var allTime = allOrders;

        return new ProfitSummaryDto
        {
            // Net profit
            TodayNetProfit   = CalculateTotalNetProfit(today, productCostFallback),
            MonthlyNetProfit = CalculateTotalNetProfit(monthly, productCostFallback),
            YearlyNetProfit  = CalculateTotalNetProfit(yearly, productCostFallback),
            TotalNetProfit   = CalculateTotalNetProfit(allTime, productCostFallback),

            // Revenue (total amount paid after all discounts)
            TodayRevenue   = today.Sum(x   => x.Order.TotalAmount),
            MonthlyRevenue = monthly.Sum(x => x.Order.TotalAmount),
            YearlyRevenue  = yearly.Sum(x  => x.Order.TotalAmount),
            TotalRevenue   = allTime.Sum(x => x.Order.TotalAmount),

            // Cost (item costs + taxi costs)
            TodayCost   = CalculateTotalCost(today, productCostFallback),
            MonthlyCost = CalculateTotalCost(monthly, productCostFallback),
            YearlyCost  = CalculateTotalCost(yearly, productCostFallback),
            TotalCost   = CalculateTotalCost(allTime, productCostFallback)
        };
    }

    public async Task<ProfitByDateRangeDto> GetProfitByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var start = startDate.Date;
        var end   = endDate.Date.AddDays(1).AddTicks(-1);

        var (allOrders, productCostFallback) = await GetDeliveredOrdersWithItemsAsync(cancellationToken);

        var inRange = allOrders
            .Where(o => o.Order.DeliveredAt >= start && o.Order.DeliveredAt <= end)
            .ToList();

        var totalRevenue   = inRange.Sum(x => x.Order.TotalAmount);
        var totalCost      = CalculateTotalCost(inRange, productCostFallback);
        var totalNetProfit = CalculateTotalNetProfit(inRange, productCostFallback);

        return new ProfitByDateRangeDto
        {
            StartDate              = start,
            EndDate                = endDate.Date,
            OrderCount             = inRange.Count,
            TotalRevenue           = totalRevenue,
            TotalCost              = totalCost,
            TotalNetProfit         = totalNetProfit,
            AverageNetProfitPerOrder = inRange.Count > 0
                ? Math.Round(totalNetProfit / inRange.Count, 2)
                : 0
        };
    }

    public async Task<IEnumerable<ProfitOrderDto>> GetProfitPerOrderAsync(CancellationToken cancellationToken = default)
    {
        var (allOrders, productCostFallback) = await GetDeliveredOrdersWithItemsAsync(cancellationToken);

        var result = allOrders
            .OrderByDescending(x => x.Order.DeliveredAt)
            .Select(x => MapToOrderProfitDto(x.Order, x.Items, productCostFallback))
            .ToList();

        return result;
    }

    public async Task UpdateOrderTaxiCostAsync(Guid orderId, decimal taxiCost, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId, cancellationToken);
        if (order == null)
            throw new KeyNotFoundException("Order not found.");

        order.TaxiCost = taxiCost;
        _unitOfWork.Repository<Order>().Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches all delivered orders + their items, plus builds a product cost lookup
    /// used as a fallback for legacy orders where UnitCostPrice was not snapshotted.
    /// </summary>
    private async Task<(List<(Order Order, IEnumerable<OrderItem> Items)> Orders, Dictionary<Guid, decimal?> ProductCostFallback)>
        GetDeliveredOrdersWithItemsAsync(CancellationToken cancellationToken)
    {
        var deliveredOrders = await _unitOfWork.Repository<Order>()
            .FindAsync(o => o.Status == OrderStatus.Delivered, cancellationToken);

        // Build product cost fallback: current PurchasePrice for products that exist
        // Used only when OrderItem.UnitCostPrice is null (orders placed before feature was added)
        var allProducts = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var productCostFallback = allProducts.ToDictionary(p => p.Id, p => p.PurchasePrice);

        var result = new List<(Order, IEnumerable<OrderItem>)>();

        foreach (var order in deliveredOrders)
        {
            var items = await _unitOfWork.Repository<OrderItem>()
                .FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
            result.Add((order, items));
        }

        return (result, productCostFallback);
    }

    /// <summary>
    /// Resolves the effective cost per unit for an order item.
    /// Priority: snapshotted UnitCostPrice → current Product.PurchasePrice → 0
    /// </summary>
    private static decimal GetEffectiveCostPerUnit(OrderItem item, Dictionary<Guid, decimal?> productCostFallback)
    {
        // 1. Use snapshotted cost (set at order time — most accurate)
        if (item.UnitCostPrice.HasValue)
            return item.UnitCostPrice.Value;

        // 2. Fall back to current product purchase price (for legacy orders)
        if (productCostFallback.TryGetValue(item.ProductId, out var fallbackCost) && fallbackCost.HasValue)
            return fallbackCost.Value;

        // 3. No cost data available — treat as 0
        return 0m;
    }

    /// <summary>
    /// Net profit formula per order:
    ///   GrossProfit  = Sum((UnitPrice - EffectiveCostPerUnit) × Quantity)
    ///   NetProfit    = GrossProfit - WalletDiscount - PromoDiscount - TaxiCost
    ///
    /// PromoDiscount must be subtracted because we calculate from UnitPrice (pre-promo).
    /// The promo is real lost revenue — the customer paid less than UnitPrice.
    /// </summary>
    private static decimal CalculateOrderNetProfit(
        Order order,
        IEnumerable<OrderItem> items,
        Dictionary<Guid, decimal?> productCostFallback)
    {
        decimal grossItemProfit = items.Sum(item =>
        {
            var costPerUnit = GetEffectiveCostPerUnit(item, productCostFallback);
            return (item.UnitPrice - costPerUnit) * item.Quantity;
        });

        decimal walletDiscount = order.WalletDiscount    ?? 0m;
        decimal promoDiscount  = order.PromoCodeDiscount ?? 0m; // Real lost revenue — must be deducted
        decimal taxiCost       = order.TaxiCost          ?? 0m;

        return grossItemProfit - walletDiscount - promoDiscount - taxiCost;
    }

    private static decimal CalculateTotalNetProfit(
        IEnumerable<(Order Order, IEnumerable<OrderItem> Items)> orders,
        Dictionary<Guid, decimal?> productCostFallback)
        => orders.Sum(x => CalculateOrderNetProfit(x.Order, x.Items, productCostFallback));

    private static decimal CalculateTotalCost(
        IEnumerable<(Order Order, IEnumerable<OrderItem> Items)> orders,
        Dictionary<Guid, decimal?> productCostFallback)
        => orders.Sum(x =>
            x.Items.Sum(item => GetEffectiveCostPerUnit(item, productCostFallback) * item.Quantity)
            + (x.Order.TaxiCost ?? 0m));

    private static ProfitOrderDto MapToOrderProfitDto(
        Order order,
        IEnumerable<OrderItem> items,
        Dictionary<Guid, decimal?> productCostFallback)
    {
        var itemsList = items.ToList();

        decimal orderCostTotal = itemsList.Sum(i => GetEffectiveCostPerUnit(i, productCostFallback) * i.Quantity);
        decimal orderNetProfit = CalculateOrderNetProfit(order, itemsList, productCostFallback);

        var itemDtos = itemsList.Select(i =>
        {
            var effectiveCost = GetEffectiveCostPerUnit(i, productCostFallback);
            return new ProfitOrderItemDto
            {
                ProductId        = i.ProductId,
                ProductName      = i.ProductName,
                ProductSku       = i.ProductSku,
                Quantity         = i.Quantity,
                UnitPrice        = i.UnitPrice,
                UnitCostPrice    = effectiveCost, // Show effective cost (snapshotted or fallback)
                NetProfitPerItem = (i.UnitPrice - effectiveCost) * i.Quantity
            };
        }).ToList();

        return new ProfitOrderDto
        {
            OrderId          = order.Id,
            OrderNumber      = order.OrderNumber,
            CustomerName     = order.CustomerName,
            OrderDate        = order.CreatedAt,
            DeliveredAt      = order.DeliveredAt,
            OrderSubTotal    = order.SubTotal,
            OrderTotalAmount = order.TotalAmount,
            OrderCostTotal   = orderCostTotal + (order.TaxiCost ?? 0m),
            OrderNetProfit   = orderNetProfit,
            WalletDiscount   = order.WalletDiscount,
            PromoDiscount    = order.PromoCodeDiscount,
            TaxiCost         = order.TaxiCost,
            Items            = itemDtos
        };
    }
}
