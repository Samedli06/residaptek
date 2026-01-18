namespace SmartTeam.Application.DTOs;

public class CartDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPriceBeforeDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    public int TotalQuantity { get; set; }
    
    // Promo code fields
    public string? AppliedPromoCode { get; set; }
    public decimal? PromoCodeDiscountPercentage { get; set; }
    public decimal PromoCodeDiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddToCartDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class AddToCartRequestDto
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

public class WhatsAppOrderDto
{
    public Guid? CartId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "AZN";
}

public class WhatsAppLinkDto
{
    public string WhatsAppUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class QuickOrderDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
}

public class QuickOrderRequestDto
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
}