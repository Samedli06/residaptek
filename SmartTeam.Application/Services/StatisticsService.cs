using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public StatisticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);
        var last30Days = now.AddDays(-30);

        // Get all data needed for calculations
        var allOrders = await _unitOfWork.Repository<Order>().GetAllAsync(cancellationToken);
        var allUsers = await _unitOfWork.Repository<User>().GetAllAsync(cancellationToken);
        var allProducts = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var allWallets = await _unitOfWork.Repository<UserWallet>().GetAllAsync(cancellationToken);
        var allTransactions = await _unitOfWork.Repository<WalletTransaction>().GetAllAsync(cancellationToken);

        var statistics = new DashboardStatisticsDto
        {
            Revenue = await CalculateRevenueStatsAsync(allOrders, todayStart, monthStart),
            Orders = CalculateOrderStats(allOrders, todayStart),
            Customers = await CalculateCustomerStatsAsync(allUsers, allOrders, monthStart, lastMonthStart),
            Products = await CalculateProductStatsAsync(allProducts, allOrders, cancellationToken),
            Bonus = CalculateBonusStats(allOrders, allTransactions),
            PromoCode = CalculatePromoStats(allOrders),
            Growth = CalculateGrowthStats(allOrders, allUsers, monthStart, lastMonthStart),
            RevenueTrend = CalculateRevenueTrend(allOrders, last30Days)
        };

        return statistics;
    }

    private async Task<RevenueStatsDto> CalculateRevenueStatsAsync(
        IEnumerable<Order> allOrders,
        DateTime todayStart,
        DateTime monthStart)
    {
        var deliveredOrders = allOrders.Where(o => o.Status == OrderStatus.Delivered).ToList();

        var todayRevenue = deliveredOrders
            .Where(o => o.DeliveredAt >= todayStart)
            .Sum(o => o.TotalAmount);

        var monthRevenue = deliveredOrders
            .Where(o => o.DeliveredAt >= monthStart)
            .Sum(o => o.TotalAmount);

        var allTimeRevenue = deliveredOrders.Sum(o => o.TotalAmount);

        var averageOrderValue = deliveredOrders.Any()
            ? deliveredOrders.Average(o => o.TotalAmount)
            : 0;

        return new RevenueStatsDto
        {
            Today = todayRevenue,
            ThisMonth = monthRevenue,
            AllTime = allTimeRevenue,
            AverageOrderValue = Math.Round(averageOrderValue, 2)
        };
    }

    private OrderStatsDto CalculateOrderStats(IEnumerable<Order> allOrders, DateTime todayStart)
    {
        var ordersList = allOrders.ToList();

        return new OrderStatsDto
        {
            TodayOrders = ordersList.Count(o => o.CreatedAt >= todayStart),
            TotalOrders = ordersList.Count,
            PendingOrders = ordersList.Count(o => o.Status == OrderStatus.Pending),
            ConfirmedOrders = ordersList.Count(o => o.Status == OrderStatus.Confirmed),
            DeliveredOrders = ordersList.Count(o => o.Status == OrderStatus.Delivered),
            CancelledOrders = ordersList.Count(o => o.Status == OrderStatus.Cancelled)
        };
    }

    private async Task<CustomerStatsDto> CalculateCustomerStatsAsync(
        IEnumerable<User> allUsers,
        IEnumerable<Order> allOrders,
        DateTime monthStart,
        DateTime lastMonthStart)
    {
        var usersList = allUsers.ToList();
        var ordersList = allOrders.ToList();

        // Active customers = users who have placed at least one order
        var usersWithOrders = ordersList.Select(o => o.UserId).Distinct().ToList();
        var activeCustomers = usersWithOrders.Count;

        return new CustomerStatsDto
        {
            TotalCustomers = usersList.Count,
            ActiveCustomers = activeCustomers
        };
    }

    private async Task<ProductStatsDto> CalculateProductStatsAsync(
        IEnumerable<Product> allProducts,
        IEnumerable<Order> allOrders,
        CancellationToken cancellationToken)
    {
        var productsList = allProducts.ToList();
        var lowStockThreshold = 10; // Products with stock < 10

        // Get all order items
        var allOrderItems = new List<OrderItem>();
        foreach (var order in allOrders.Where(o => o.Status == OrderStatus.Delivered))
        {
            var items = await _unitOfWork.Repository<OrderItem>()
                .FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
            allOrderItems.AddRange(items);
        }

        // Calculate best selling products
        var productSales = allOrderItems
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.TotalPrice)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToList();

        var bestSelling = new List<TopProductDto>();
        foreach (var sale in productSales)
        {
            var product = productsList.FirstOrDefault(p => p.Id == sale.ProductId);
            if (product != null)
            {
                bestSelling.Add(new TopProductDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    QuantitySold = sale.QuantitySold,
                    Revenue = sale.Revenue
                });
            }
        }

        return new ProductStatsDto
        {
            TotalProducts = productsList.Count,
            LowStockProducts = productsList.Count(p => p.StockQuantity < lowStockThreshold && p.StockQuantity > 0),
            BestSelling = bestSelling
        };
    }

    private BonusStatsDto CalculateBonusStats(
        IEnumerable<Order> allOrders,
        IEnumerable<WalletTransaction> allTransactions)
    {
        var ordersList = allOrders.ToList();
        var transactionsList = allTransactions.ToList();

        // Total bonus awarded from delivered orders
        var totalAwarded = ordersList
            .Where(o => o.BonusAwarded && o.BonusAmount.HasValue)
            .Sum(o => o.BonusAmount.Value);

        // Total bonus used (debit transactions)
        var totalUsed = transactionsList
            .Where(t => t.Type == TransactionType.Debit)
            .Sum(t => t.Amount);

        // Active balance = awarded - used
        var activeBalance = totalAwarded - totalUsed;

        return new BonusStatsDto
        {
            TotalAwarded = totalAwarded,
            TotalUsed = totalUsed,
            ActiveBalance = activeBalance
        };
    }

    private PromoStatsDto CalculatePromoStats(IEnumerable<Order> allOrders)
    {
        var ordersWithPromo = allOrders
            .Where(o => o.PromoCodeDiscount.HasValue && o.PromoCodeDiscount.Value > 0)
            .ToList();

        var totalUsageCount = ordersWithPromo.Count;
        var totalDiscountGiven = ordersWithPromo.Sum(o => o.PromoCodeDiscount.Value);
        var averageDiscount = totalUsageCount > 0
            ? totalDiscountGiven / totalUsageCount
            : 0;

        return new PromoStatsDto
        {
            TotalUsageCount = totalUsageCount,
            TotalDiscountGiven = totalDiscountGiven,
            AverageDiscountPerOrder = Math.Round(averageDiscount, 2)
        };
    }

    private GrowthStatsDto CalculateGrowthStats(
        IEnumerable<Order> allOrders,
        IEnumerable<User> allUsers,
        DateTime monthStart,
        DateTime lastMonthStart)
    {
        var ordersList = allOrders.ToList();
        var usersList = allUsers.ToList();

        // Current month stats
        var currentMonthOrders = ordersList.Where(o => o.CreatedAt >= monthStart).ToList();
        var currentMonthRevenue = currentMonthOrders
            .Where(o => o.Status == OrderStatus.Delivered)
            .Sum(o => o.TotalAmount);
        var currentMonthCustomers = usersList.Count(u => u.CreatedAt >= monthStart);

        // Last month stats
        var lastMonthOrders = ordersList
            .Where(o => o.CreatedAt >= lastMonthStart && o.CreatedAt < monthStart)
            .ToList();
        var lastMonthRevenue = lastMonthOrders
            .Where(o => o.Status == OrderStatus.Delivered)
            .Sum(o => o.TotalAmount);
        var lastMonthCustomers = usersList
            .Count(u => u.CreatedAt >= lastMonthStart && u.CreatedAt < monthStart);

        // Calculate growth percentages
        var revenueGrowth = lastMonthRevenue > 0
            ? ((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
            : 0;

        var ordersGrowth = lastMonthOrders.Count > 0
            ? ((currentMonthOrders.Count - lastMonthOrders.Count) * 100) / lastMonthOrders.Count
            : 0;

        var customersGrowth = lastMonthCustomers > 0
            ? ((currentMonthCustomers - lastMonthCustomers) * 100) / lastMonthCustomers
            : 0;

        return new GrowthStatsDto
        {
            RevenueGrowthPercent = Math.Round(revenueGrowth, 1),
            OrdersGrowthPercent = ordersGrowth,
            CustomersGrowthPercent = customersGrowth
        };
    }

    private List<RevenueTrendDto> CalculateRevenueTrend(
        IEnumerable<Order> allOrders,
        DateTime startDate)
    {
        var deliveredOrders = allOrders
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt >= startDate)
            .ToList();

        var trend = deliveredOrders
            .GroupBy(o => o.DeliveredAt.Value.Date)
            .Select(g => new RevenueTrendDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(t => t.Date)
            .ToList();

        return trend;
    }
}
