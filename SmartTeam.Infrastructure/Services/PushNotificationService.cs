using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private const string ExpoEndpoint = "https://exp.host/--/api/v2/push/send";
    private const int BatchSize = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        ILogger<PushNotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────────

    public async Task SendToUserAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var tokenEntities = await _unitOfWork.Repository<UserPushToken>()
            .FindAsync(t => t.UserId == userId, cancellationToken);

        var tokens = tokenEntities.Select(t => t.Token).ToList();
        if (!tokens.Any())
        {
            _logger.LogDebug("No push tokens found for user {UserId}", userId);
            return;
        }

        await SendToTokensAsync(tokens, title, body, data, cancellationToken);
    }

    public async Task SendToAllAsync(
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var allTokenEntities = await _unitOfWork.Repository<UserPushToken>()
            .GetAllAsync(cancellationToken);

        var tokens = allTokenEntities.Select(t => t.Token).ToList();
        if (!tokens.Any())
        {
            _logger.LogDebug("No push tokens registered — skipping broadcast");
            return;
        }

        await SendToTokensAsync(tokens, title, body, data, cancellationToken);
    }

    public async Task SendToTokensAsync(
        IEnumerable<string> tokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var tokenList = tokens.Distinct().ToList();
        if (!tokenList.Any()) return;

        // Split into batches of 100 (Expo hard limit)
        var batches = tokenList
            .Select((token, index) => new { token, index })
            .GroupBy(x => x.index / BatchSize)
            .Select(g => g.Select(x => x.token).ToList())
            .ToList();

        var staleTokens = new List<string>();

        foreach (var batch in batches)
        {
            var stale = await SendBatchAsync(batch, title, body, data, cancellationToken);
            staleTokens.AddRange(stale);
        }

        if (staleTokens.Any())
        {
            await RemoveStaleTokensAsync(staleTokens, cancellationToken);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends one batch (≤ 100 messages) to the Expo push gateway.
    /// Returns the list of tokens Expo flagged as stale/unregistered.
    /// </summary>
    private async Task<List<string>> SendBatchAsync(
        List<string> tokens,
        string title,
        string body,
        Dictionary<string, string>? data,
        CancellationToken cancellationToken)
    {
        var messages = tokens.Select(token => new
        {
            to = token,
            title,
            body,
            sound = "default",
            data = data ?? new Dictionary<string, string>()
        }).ToList();

        var json = JsonSerializer.Serialize(messages);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var client = _httpClientFactory.CreateClient("ExpoClient");
            var response = await client.PostAsync(ExpoEndpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Expo push API returned {StatusCode}: {Body}",
                    (int)response.StatusCode, responseBody);
                return new List<string>();
            }

            return ExtractStaleTokens(tokens, responseBody);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while sending push notifications to Expo");
            return new List<string>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Push notification request timed out");
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending push notifications");
            return new List<string>();
        }
    }

    /// <summary>
    /// Parses Expo's response JSON and returns tokens whose status is
    /// "error" with error code "DeviceNotRegistered".
    /// </summary>
    private List<string> ExtractStaleTokens(List<string> sentTokens, string responseBody)
    {
        var stale = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (!doc.RootElement.TryGetProperty("data", out var dataArray))
                return stale;

            var results = dataArray.EnumerateArray().ToList();
            for (int i = 0; i < results.Count && i < sentTokens.Count; i++)
            {
                var result = results[i];
                if (result.TryGetProperty("status", out var status) &&
                    status.GetString() == "error" &&
                    result.TryGetProperty("details", out var details) &&
                    details.TryGetProperty("error", out var error) &&
                    error.GetString() == "DeviceNotRegistered")
                {
                    stale.Add(sentTokens[i]);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Expo push response for stale token detection");
        }

        return stale;
    }

    /// <summary>
    /// Removes stale / unregistered tokens from the database.
    /// </summary>
    private async Task RemoveStaleTokensAsync(
        List<string> staleTokens,
        CancellationToken cancellationToken)
    {
        try
        {
            var entities = await _unitOfWork.Repository<UserPushToken>()
                .FindAsync(t => staleTokens.Contains(t.Token), cancellationToken);

            if (!entities.Any()) return;

            _unitOfWork.Repository<UserPushToken>().RemoveRange(entities);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Removed {Count} stale push token(s) from the database",
                entities.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove stale push tokens");
        }
    }
}
