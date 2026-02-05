namespace SmartTeam.Application.DTOs;

public class UserBonusDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PharmacyName { get; set; }
    public decimal BonusBalance { get; set; }
    public DateTime? LastBonusEarned { get; set; }
    public decimal TotalBonusEarned { get; set; }
    public decimal TotalBonusUsed { get; set; }
}
