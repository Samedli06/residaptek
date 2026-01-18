using SmartTeam.Domain.Entities;
using System.Security.Claims;

namespace SmartTeam.Application.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiry();
}
