namespace SmartTeam.Application.Services;

public interface IPushNotificationService
{
    /// <summary>
    /// Send a push notification to all registered tokens of a specific user.
    /// </summary>
    Task SendToUserAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a push notification to ALL registered tokens (global broadcast).
    /// Automatically batches into groups of 100 (Expo limit).
    /// </summary>
    Task SendToAllAsync(
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a push notification to an explicit list of tokens.
    /// Automatically batches into groups of 100 (Expo limit).
    /// </summary>
    Task SendToTokensAsync(
        IEnumerable<string> tokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
