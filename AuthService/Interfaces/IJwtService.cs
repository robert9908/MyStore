using System.Security.Claims;

namespace AuthService.Interfaces
{
    public interface IJwtService
    {
        DateTime GetExpiryFromToken(string token);
        string GenerateAccessToken(User user);
        ClaimsPrincipal? ValidateToken(string token);
        public (string AccessToken, string RefreshToken) GenerateTokens(User user);
    }
}
