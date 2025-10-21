
using AuthService.Configurations;
using AuthService.Interfaces;
using AuthService.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Services
{
    public class JwtService : IJwtService
    {
        private readonly IOptions<JwtSettings> _jwtSettings;
        private readonly ILogger<JwtService> _logger;
        private readonly IRefreshTokenService _refreshTokenService;

        public JwtService(IOptions<JwtSettings> jwtSettiings, ILogger<JwtService> logger, IRefreshTokenService refreshTokenService)
        {
            _jwtSettings = jwtSettiings;
            _logger = logger;
            _refreshTokenService = refreshTokenService;
        }

        public string GenerateAccessToken(User user)
        {
            if(user is null) throw new ArgumentNullException(nameof(user));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("sub", user.Id.ToString()),
                new("email", user.Email),
                new("role", user.Role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.Value.AccessTokenExpiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string AccessToken, string RefreshToken) GenerateTokens(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            return (accessToken, refreshToken);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Value.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Value.Audience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParams, out _);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT validation failed");
                return null;
            }
        }

        public DateTime GetExpiryFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Value.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    return jwtToken.ValidTo.ToUniversalTime();
                }

                throw new SecurityTokenException("Invalid token structure");
            }

            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid JWT Token: {Message}", ex.Message);
                throw new SecurityTokenException("Token validation failed", ex);

            }
        }
    }
}
