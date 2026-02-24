using System.ComponentModel.DataAnnotations;

namespace SmartTeam.Domain.Entities;

public class GlobalSettings
{
    [Key]
    public Guid Id { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public decimal MinimumOrderAmount { get; set; } = 0;

    public bool IsMinimumOrderAmountEnabled { get; set; } = false;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
