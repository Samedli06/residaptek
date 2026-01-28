using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeliveryNotes { get; set; }
    public decimal? UseWalletAmount { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string PharmacyName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeliveryNotes { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal? PromoCodeDiscount { get; set; }
    public decimal? WalletDiscount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool BonusAwarded { get; set; }
    public decimal? BonusAmount { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ProductImageUrl { get; set; }
}

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}

public class OrderListDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PharmacyName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ItemsCount { get; set; }
}
