using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.Services;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IUnitOfWork unitOfWork,
        ILogger<NotificationsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Register (or update) the Expo push token for the currently authenticated user.
    /// This endpoint is idempotent — calling it with an already-registered token is a no-op.
    /// </summary>
    /// <remarks>
    /// Call this from the mobile app every time the app launches and obtains an Expo push token.
    /// </remarks>
    [HttpPost("register-push-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterPushToken(
        [FromBody] RegisterPushTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
        {
            return BadRequest(new { message = "Push token is required." });
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        // Check for duplicate — same user, same token
        var exists = await _unitOfWork.Repository<UserPushToken>()
            .AnyAsync(t => t.UserId == userId && t.Token == request.Token, cancellationToken);

        if (!exists)
        {
            var entity = new UserPushToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = request.Token.Trim(),
                Platform = request.Platform?.ToLower(),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserPushToken>().AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Registered push token for user {UserId} (platform: {Platform})",
                userId, entity.Platform ?? "unknown");
        }
        else
        {
            _logger.LogDebug("Push token already registered for user {UserId} — skipping", userId);
        }

        return Ok(new { message = "Push token registered successfully." });
    }

    /// <summary>
    /// Remove a specific push token for the currently authenticated user (e.g. on logout).
    /// </summary>
    [HttpDelete("push-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemovePushToken(
        [FromBody] RegisterPushTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
        {
            return BadRequest(new { message = "Push token is required." });
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        var entity = await _unitOfWork.Repository<UserPushToken>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == request.Token, cancellationToken);

        if (entity != null)
        {
            _unitOfWork.Repository<UserPushToken>().Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { message = "Push token removed." });
    }
}

/// <summary>Request body for push token registration.</summary>
public class RegisterPushTokenRequest
{
    /// <summary>Expo push token, e.g. ExponentPushToken[xxxxxxxxxxxxxxxx]</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Optional device platform: "ios" or "android"</summary>
    public string? Platform { get; set; }
}
