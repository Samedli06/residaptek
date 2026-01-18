namespace SmartTeam.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Promo code fields
    public Guid? AppliedPromoCodeId { get; set; }
    public PromoCode? AppliedPromoCode { get; set; }
    public decimal? PromoCodeDiscountPercentage { get; set; }
    
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    public ICollection<PromoCodeUsage> PromoCodeUsages { get; set; } = new List<PromoCodeUsage>();
    
    // Calculated properties
    public decimal SubTotal => Items.Sum(item => item.TotalPrice);
    public decimal PromoCodeDiscountAmount => PromoCodeDiscountPercentage.HasValue 
        ? SubTotal * (PromoCodeDiscountPercentage.Value / 100) 
        : 0;
    public decimal TotalAmount => SubTotal - PromoCodeDiscountAmount;
    public int TotalQuantity => Items.Sum(item => item.Quantity);
}

public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Calculated properties
    public decimal TotalPrice => UnitPrice * Quantity;
}
