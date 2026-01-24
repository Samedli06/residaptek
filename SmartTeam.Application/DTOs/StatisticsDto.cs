using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class DashboardStatisticsDto
{
    public RevenueStatsDto Revenue { get; set; } = new();
    public OrderStatsDto Orders { get; set; } = new();
    public CustomerStatsDto Customers { get; set; } = new();
    public ProductStatsDto Products { get; set; } = new();
    public BonusStatsDto Bonus { get; set; } = new();
    public PromoStatsDto PromoCode { get; set; } = new();
    public GrowthStatsDto Growth { get; set; } = new();
    public List<RevenueTrendDto> RevenueTrend { get; set; } = new();
}

public class RevenueStatsDto
{
    public decimal Today { get; set; }
    public decimal ThisMonth { get; set; }
    public decimal AllTime { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class OrderStatsDto
{
    public int TodayOrders { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
}

public class CustomerStatsDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
}

public class ProductStatsDto
{
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public List<TopProductDto> BestSelling { get; set; } = new();
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class BonusStatsDto
{
    public decimal TotalAwarded { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal ActiveBalance { get; set; }
}

public class PromoStatsDto
{
    public int TotalUsageCount { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal AverageDiscountPerOrder { get; set; }
}

public class GrowthStatsDto
{
    public decimal RevenueGrowthPercent { get; set; }
    public int OrdersGrowthPercent { get; set; }
    public int CustomersGrowthPercent { get; set; }
}

public class RevenueTrendDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}
