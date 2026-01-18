namespace SmartTeam.Domain.Entities;

public class PromoCode
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? UsageLimit { get; set; }
    public int CurrentUsageCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<PromoCodeUsage> Usages { get; set; } = new List<PromoCodeUsage>();
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
}

public class PromoCodeUsage
{
    public Guid Id { get; set; }
    public Guid PromoCodeId { get; set; }
    public PromoCode PromoCode { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? CartId { get; set; }
    public Cart? Cart { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime UsedAt { get; set; }
}
