using AutoMapper;
using SmartTeam.Application.Configuration;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace SmartTeam.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IJwtService jwtService,
        IPasswordService passwordService,
        IEmailService emailService,
        IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _emailService = emailService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        var existingUser = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }
        if (!string.IsNullOrEmpty(registerDto.PhoneNumber))
        {
            var existingPhoneUser = await _unitOfWork.Repository<User>()
                .FirstOrDefaultAsync(u => u.PhoneNumber == registerDto.PhoneNumber, cancellationToken);

            if (existingPhoneUser != null)
            {
                throw new InvalidOperationException("User with this phone number already exists.");
            }
        }

        // Validate password confirmation
        if (registerDto.Password != registerDto.ConfirmPassword)
        {
            throw new ArgumentException("Password and confirmation password do not match.");
        }

        // Create new user with NormalUser role (default)
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            PhoneNumber = registerDto.PhoneNumber,
            PasswordHash = _passwordService.HashPassword(registerDto.Password),
            Role = UserRole.NormalUser, // Default role for new registrations
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryInDays),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            User = _mapper.Map<UserDto>(user),
            ExpiresAt = _jwtService.GetTokenExpiry()
        };
    }

    public async Task<LoginSuccessDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Generate tokens (stored internally, not returned to client)
        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Save refresh token for future use
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryInDays),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return success message with token for client authentication
        return new LoginSuccessDto
        {
            Message = "Login successful",
            Success = true,
            Token = token,
            ExpiresAt = _jwtService.GetTokenExpiry()
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default)
    {
        var principal = _jwtService.ValidateToken(refreshTokenDto.Token);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }

        var refreshTokenEntity = await _unitOfWork.Repository<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken && rt.UserId == userId && !rt.IsRevoked, cancellationToken);

        if (refreshTokenEntity == null || refreshTokenEntity.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        // Revoke old refresh token
        refreshTokenEntity.IsRevoked = true;
        refreshTokenEntity.RevokedAt = DateTime.UtcNow;
        _unitOfWork.Repository<RefreshToken>().Update(refreshTokenEntity);

        // Generate new tokens
        var newToken = _jwtService.GenerateToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Save new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryInDays),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            User = _mapper.Map<UserDto>(user),
            ExpiresAt = _jwtService.GetTokenExpiry()
        };
    }

    public async Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return false;
        }

        var refreshTokens = await _unitOfWork.Repository<RefreshToken>()
            .FindAsync(rt => rt.UserId == userGuid && !rt.IsRevoked, cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        _unitOfWork.Repository<RefreshToken>().UpdateRange(refreshTokens);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<UserDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ArgumentException("Invalid user ID.");
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userGuid, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ArgumentException("Invalid user ID.");
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userGuid, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        if (!_passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
        {
            throw new ArgumentException("New password and confirmation do not match.");
        }

        user.PasswordHash = _passwordService.HashPassword(changePasswordDto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var principal = _jwtService.ValidateToken(token);
        return principal != null;
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email && u.IsActive, cancellationToken);

        // Always return success to prevent user enumeration
        var response = new ForgotPasswordResponseDto
        {
            Message = "Əgər bu e-poçt ünvanı sistemdə qeydiyyatdan keçibsə, şifrə sıfırlama linki göndəriləcək.",
            Success = true
        };

        if (user == null)
        {
            return response;
        }

        // Invalidate any existing reset tokens for this user
        var existingTokens = await _unitOfWork.Repository<PasswordResetToken>()
            .FindAsync(prt => prt.UserId == user.Id && !prt.IsUsed, cancellationToken);

        foreach (var token in existingTokens)
        {
            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
        }

        if (existingTokens.Any())
        {
            _unitOfWork.Repository<PasswordResetToken>().UpdateRange(existingTokens);
        }

        // Generate new reset token
        var resetToken = GenerateSecureToken();
        var resetTokenEntity = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<PasswordResetToken>().AddAsync(resetTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email
        var emailSent = await _emailService.SendPasswordResetEmailAsync(
            user.Email, 
            resetToken, 
            $"{user.FirstName} {user.LastName}", 
            cancellationToken);

        if (!emailSent)
        {
            response.Message = "E-poçt göndərilmədi. Zəhmət olmasa daha sonra yenidən cəhd edin.";
            response.Success = false;
        }

        return response;
    }

    public async Task<ForgotPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken = default)
    {
        var resetToken = await _unitOfWork.Repository<PasswordResetToken>()
            .FirstOrDefaultAsync(prt => prt.Token == resetPasswordDto.Token && !prt.IsUsed, cancellationToken);

        if (resetToken == null || resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            return new ForgotPasswordResponseDto
            {
                Message = "Şifrə sıfırlama linki etibarsızdır və ya müddəti bitib. Zəhmət olmasa yeni tələb göndərin.",
                Success = false
            };
        }

        if (resetPasswordDto.NewPassword != resetPasswordDto.ConfirmNewPassword)
        {
            return new ForgotPasswordResponseDto
            {
                Message = "Yeni şifrə və təsdiq şifrəsi uyğun gəlmir.",
                Success = false
            };
        }

        if (resetPasswordDto.NewPassword.Length < 6)
        {
            return new ForgotPasswordResponseDto
            {
                Message = "Şifrə ən azı 6 simvol olmalıdır.",
                Success = false
            };
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return new ForgotPasswordResponseDto
            {
                Message = "İstifadəçi tapılmadı və ya hesab deaktivdir.",
                Success = false
            };
        }

        // Update password
        user.PasswordHash = _passwordService.HashPassword(resetPasswordDto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Mark token as used
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;

        _unitOfWork.Repository<User>().Update(user);
        _unitOfWork.Repository<PasswordResetToken>().Update(resetToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResponseDto
        {
            Message = "Şifrəniz uğurla yeniləndi. İndi yeni şifrə ilə daxil ola bilərsiniz.",
            Success = true
        };
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
