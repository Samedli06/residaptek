namespace SmartTeam.Application.DTOs;

public class GlobalSettingsDto
{
    public decimal MinimumOrderAmount { get; set; }
    public bool IsMinimumOrderAmountEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateGlobalSettingsDto
{
    public decimal MinimumOrderAmount { get; set; }
    public bool IsMinimumOrderAmountEnabled { get; set; }
}
