namespace SmartTeam.Application.DTOs;

public class PromoCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; }
    public int? UsageLimit { get; set; }
    public int CurrentUsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PromoCodeListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; }
    public int CurrentUsageCount { get; set; }
    public int? UsageLimit { get; set; }
}

public class CreatePromoCodeDto
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? UsageLimit { get; set; }
}

public class UpdatePromoCodeDto
{
    public string? Code { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool? IsActive { get; set; }
    public int? UsageLimit { get; set; }
}

public class ApplyPromoCodeDto
{
    public string PromoCode { get; set; } = string.Empty;
}

public class PromoCodeValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public PromoCodeDto? PromoCode { get; set; }
}

public class PromoCodeUsageDto
{
    public Guid Id { get; set; }
    public Guid PromoCodeId { get; set; }
    public string PromoCodeName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? CartId { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime UsedAt { get; set; }
}
