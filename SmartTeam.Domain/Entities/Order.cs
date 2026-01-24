namespace SmartTeam.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty; // Auto-generated unique order number
    
    // User Information
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Delivery Information
    public string DeliveryAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeliveryNotes { get; set; }
    
    // Order Details
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public decimal SubTotal { get; set; }
    public decimal? PromoCodeDiscount { get; set; }
    public decimal? WalletDiscount { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Status & Tracking
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    // Bonus Tracking
    public bool BonusAwarded { get; set; } = false;
    public decimal? BonusAmount { get; set; }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // Product snapshot at time of order
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Delivered = 2,
    Cancelled = 3
}
