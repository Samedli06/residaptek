using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;
using System.Security.Claims;

namespace SmartTeam.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    /// <summary>
    /// Test endpoint to verify connection string (REMOVE IN PRODUCTION)
    /// </summary>
    [HttpGet("test-connection")]
    public IActionResult TestConnection()
    {
        var connString = _configuration.GetConnectionString("DefaultConnection");
        // Hide password for security
        var safeConnString = System.Text.RegularExpressions.Regex.Replace(
            connString ?? "NOT FOUND", 
            @"Password=([^;]*)", 
            "Password=***HIDDEN***"
        );
        return Ok(new { 
            connectionString = safeConnString,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not Set"
        });
    }


    /// <summary>
    /// Register a new user (default role: NormalUser)
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto, CancellationToken cancellationToken)
    {
        try
        {
            if (registerDto == null)
            {
                return BadRequest(new { error = "Invalid registration data.", message = "Registration data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(registerDto.Email))
            {
                return BadRequest(new { error = "Email required.", message = "Email address is required for registration." });
            }

            if (string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return BadRequest(new { error = "Password required.", message = "Password is required for registration." });
            }

            if (registerDto.Password.Length < 6)
            {
                return BadRequest(new { error = "Password too short.", message = "Password must be at least 6 characters long." });
            }

            var result = await _authService.RegisterAsync(registerDto, cancellationToken);
            return CreatedAtAction(nameof(GetCurrentUser), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Registration failed.", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid registration parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Registration failed due to server error.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginSuccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginSuccessDto>> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
    {
        try
        {
            if (loginDto == null)
            {
                return BadRequest(new { error = "Invalid login data.", message = "Login data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(loginDto.Email))
            {
                return BadRequest(new { error = "Email required.", message = "Email address is required for login." });
            }

            if (string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return BadRequest(new { error = "Password required.", message = "Password is required for login." });
            }

            var result = await _authService.LoginAsync(loginDto, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "Login failed.", message = ex.Message, success = false });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid login parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            // Temporarily expose actual error for debugging
            return StatusCode(500, new { 
                error = "Login failed due to server error.", 
                message = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken)
    {
        try
        {
            if (refreshTokenDto == null)
            {
                return BadRequest(new { error = "Invalid refresh token data.", message = "Refresh token data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
            {
                return BadRequest(new { error = "Refresh token required.", message = "Refresh token is required." });
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "Token refresh failed.", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid refresh token parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Token refresh failed due to server error.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Logout user (revoke refresh tokens)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated.", message = "Please login to access this endpoint." });
            }

            await _authService.LogoutAsync(userId, cancellationToken);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Logout failed due to server error.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated.", message = "Please login to access this endpoint." });
            }

            var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(new { error = "User not found.", message = "Current user information could not be retrieved." });
            }

            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid user parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve user information.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated.", message = "Please login to access this endpoint." });
            }

            if (changePasswordDto == null)
            {
                return BadRequest(new { error = "Invalid password change data.", message = "Password change data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(changePasswordDto.CurrentPassword))
            {
                return BadRequest(new { error = "Current password required.", message = "Current password is required to change password." });
            }

            if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword))
            {
                return BadRequest(new { error = "New password required.", message = "New password is required." });
            }

            if (changePasswordDto.NewPassword.Length < 6)
            {
                return BadRequest(new { error = "New password too short.", message = "New password must be at least 6 characters long." });
            }

            if (changePasswordDto.CurrentPassword == changePasswordDto.NewPassword)
            {
                return BadRequest(new { error = "Same password.", message = "New password must be different from current password." });
            }

            var user = await _authService.ChangePasswordAsync(userId, changePasswordDto, cancellationToken);
            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "Password change failed.", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid password change parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Password change failed due to server error.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto, CancellationToken cancellationToken)
    {
        try
        {
            if (forgotPasswordDto == null)
            {
                return BadRequest(new { error = "Invalid forgot password data.", message = "Forgot password data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
            {
                return BadRequest(new { error = "Email required.", message = "Email address is required for password reset." });
            }

            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid forgot password parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Password reset request failed due to server error.", message = "Please try again later or contact support if the issue persists." });
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken)
    {
        try
        {
            if (resetPasswordDto == null)
            {
                return BadRequest(new { error = "Invalid reset password data.", message = "Reset password data cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(resetPasswordDto.Token))
            {
                return BadRequest(new { error = "Token required.", message = "Reset token is required." });
            }

            if (string.IsNullOrWhiteSpace(resetPasswordDto.NewPassword))
            {
                return BadRequest(new { error = "New password required.", message = "New password is required." });
            }

            if (resetPasswordDto.NewPassword.Length < 6)
            {
                return BadRequest(new { error = "New password too short.", message = "New password must be at least 6 characters long." });
            }

            if (resetPasswordDto.NewPassword != resetPasswordDto.ConfirmNewPassword)
            {
                return BadRequest(new { error = "Passwords do not match.", message = "New password and confirmation password do not match." });
            }

            var result = await _authService.ResetPasswordAsync(resetPasswordDto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Invalid reset password parameters.", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Password reset failed due to server error.", message = "Please try again later or contact support if the issue persists." });
        }
    }
}
