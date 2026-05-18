namespace SmartTeam.Domain.Entities;

public class UserPushToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Expo push token e.g. ExponentPushToken[xxxxxx]</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Optional: "ios" | "android"</summary>
    public string? Platform { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
