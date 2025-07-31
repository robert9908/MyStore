using AuthService.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash  =sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
