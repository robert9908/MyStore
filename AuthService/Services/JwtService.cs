using AuthService.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Services
{
    public class JwtService : IJwtService
    {
        public DateTime GetExpiryFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo.ToUniversalTime();
        }
    }
}
