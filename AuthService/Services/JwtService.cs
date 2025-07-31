
using AuthService.Configurations;
using AuthService.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.Value.ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string AccessToken, string RefreshToken) GenerateTokens(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = _refreshTokenService.GenerateRefreshToken();
            
            return(accessToken, refreshToken);
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
