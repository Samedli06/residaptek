namespace SmartTeam.Application.Configuration;

public class BonusSettings
{
    public decimal BonusPercentage { get; set; } = 5.0m;
    public decimal MinimumOrderForBonus { get; set; } = 0.0m;
}
