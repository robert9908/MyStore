using AuthService.DTOs;
using AuthService.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IDatabase _redis;
        private readonly AppDbContext _context;
        public readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IRateLimitService _rateLimitService;

        public AuthService(AppDbContext context, IConfiguration config, IEmailService emailService, IJwtService jwtservice, IConnectionMultiplexer redis, IRateLimitService rateLimitService )
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _jwtService = jwtservice;
            _redis = redis.GetDatabase();
            _rateLimitService = rateLimitService;
        }


        public async Task<AuthResponse> LoginAsync(DTOs.LoginRequest request, string ip)
        {
            string rateKey = $"login:attempts:{request.Email}:{ip}";
            if (await _rateLimitService.IsLimitedAsync(rateKey, 5, TimeSpan.FromMinutes(15)))
            {
                throw new Exception("Too many login attempts. Try again later");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");
            if (!user.IsEmailConfirmed) throw new Exception("Please confirm your Email first");

            user.RefreshToken = Guid.NewGuid().ToString();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var tokens = GenerateToken(user);

            return new AuthResponse
            {
                AccessToken = tokens.accessToken,
                RefreshToken = user.RefreshToken,
                Role = user.Role

            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiryTime < DateTime.Now)
                throw new Exception("invalid Refresh Token");

            user.RefreshToken = Guid.NewGuid().ToString();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var tokens = GenerateToken(user);

            return new AuthResponse
            {
                AccessToken = tokens.accessToken,
                RefreshToken = user.RefreshToken,
                Role = user.Role
            };
        }

        public async Task<AuthResponse> RegisterAsync(DTOs.RegisterRequest request)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existing != null) throw new Exception("User already exists");
            
            var passwordHash = HashPassword(request.Password);
            var user = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "Client",
                RefreshToken = Guid.NewGuid().ToString(),
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                EmailConfirmationToken = Guid.NewGuid().ToString(),
                IsEmailConfirmed = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            await _emailService.SendConfirmationEmailAsync(user.Email, user.EmailConfirmationToken);

            var tokens = GenerateToken(user);

            return new AuthResponse
            {
                Message = "Registration succesful. Please confirm your email"
            };

        }



        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash) => HashPassword(password) == hash;

        private (string accessToken, string  refreshToken) GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt: key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), user.RefreshToken);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return;

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken);
        }

        public async Task ResetPasswordAsync(DTOs.ResetPasswordRequest request, string ip)
        {
            //string rateKey = $"reset:attempts:{user.Email}:{ip}";
            //if (await _rateLimitService.IsLimitedAsync(rateKey, 3, TimeSpan.FromMinutes(30)))
           // {
            //    throw new Exception("Too many reset requests. Try again later");
            //
            var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == request.Token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null) throw new Exception("Invalid or expired reset token");


            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();
        }

        public async Task LogoutAsync(string refreshToken, string accessToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null) return;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.Now;
            await _context.SaveChangesAsync();

            var tokenExpiry = _jwtService.GetExpiryFromToken(accessToken); 
            var timeToLive = tokenExpiry - DateTime.UtcNow;

            if (timeToLive.TotalSeconds > 0)
            {
                await _redis.StringSetAsync($"blacklist:{accessToken}", "true", timeToLive);
            }
        }

        
    }
}
