using AuthService.DTOs;
using AuthService.Entities;
using AuthService.Interfaces;
using AuthService.Data;
using AuthService.Exceptions;
using AuthService.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using StackExchange.Redis;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IJwtService _jwtService;
        private readonly IDatabase _redis;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<AuthService> _logger;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IPasswordService _passwordService;

        public AuthService(
            AppDbContext context,
            IConfiguration config,
            IEmailService emailService,
            IJwtService jwtService,
            IConnectionMultiplexer redis,
            IRateLimitService rateLimitService,
            ILogger<AuthService> logger,
            IRefreshTokenService refreshTokenService,
            IPasswordService passwordService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _redis = redis.GetDatabase() ?? throw new ArgumentNullException(nameof(redis));
            _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        }


        public async Task<AuthResponse> LoginAsync(DTOs.LoginRequest request, string ip)
        {
            var rateKey = $"login:attempts:{request.Email}:{ip}";
            if (await _rateLimitService.IsLimitedAsync(rateKey))
            {
                throw new TooManyRequestsException("Too many login attempts. Try again later");
            }
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    user.LastFailedLogin = DateTime.UtcNow;
                    
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                        _logger.LogWarning("User {Email} locked out due to too many failed attempts", user.Email);
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
                // Rate limiting handled by middleware
                throw new UnauthorizedException("Invalid credentials");
            }
            
            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                throw new UnauthorizedException($"Account is locked until {user.LockoutEnd}");
            }
            
            // Reset failed attempts on successful login
            if (user.FailedLoginAttempts > 0)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
            }
            
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = ip;

            if (!user.IsEmailConfirmed) throw new InvalidOperationException("Please confirm your Email first");

            if (user.IsTwoFactorEnabled)
            {
                var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                user.TwoFactorCode = code;
                user.TwoFactorCodeExpiryTime = DateTime.UtcNow.AddMinutes(5);
                await _context.SaveChangesAsync();
                await _emailService.SendTwoFactorCodeAsync(user.Email, code);

                return new AuthResponse
                {
                    Message = "2FA code sent. Please confirmg"
                };
            }

            await _rateLimitService.ResetAttemptsAsync(rateKey);


            var (accessToken, newRefreshToken) = _jwtService.GenerateTokens(user);
            user.RefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();
            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                Role = user.Role

            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var tokenHash = _refreshTokenService.HashToken(refreshToken);
            var user = await _context.Users.SingleOrDefaultAsync(u => u.RefreshTokenHash == tokenHash);
            if (user == null || user.RefreshTokenExpiryTime < DateTime.Now)
                throw new UnauthorizedAccessException("invalid or expired refresh Token");


            var (accessToken, newRefreshToken) = _jwtService.GenerateTokens(user);

            user.RefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                Role = user.Role
            };
        }

        public async Task<AuthResponse> RegisterAsync(DTOs.RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("User already exists");

            // Validate password strength
            if (!_passwordService.IsPasswordStrong(request.Password))
            {
                throw new ValidationException("Password does not meet security requirements", new Dictionary<string, string[]>());
            }
            
            var user = new User
            {
                Email = request.Email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                Role = "Client",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                EmailConfirmationToken = Guid.NewGuid().ToString(),
                IsEmailConfirmed = false
            };

            var (accessToken, newRefreshToken) = _jwtService.GenerateTokens(user);

            user.RefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);


            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _emailService.SendConfirmationEmailAsync(user.Email, user.EmailConfirmationToken);

            return new AuthResponse
            {
                Message = "Registration succesful. Please confirm your email"
            };

        }

        public async Task ForgotPasswordAsync(DTOs.ForgotPasswordRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return;

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken);
        }

        public async Task<AuthResponse> ResetPasswordAsync(DTOs.ResetPasswordRequest request, string ip)
        {
            var rateKey = $"reset:attempts:{request.Token}:{ip}";
            if (await _rateLimitService.IsLimitedAsync(rateKey))
            {
                throw new TooManyRequestsException("Too many reset requests. Try again later");
            }
            var user = await _context.Users.SingleOrDefaultAsync(u =>
            u.PasswordResetToken == request.Token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null) throw new UnauthorizedAccessException("Invalid or expired reset token");


            if (!_passwordService.IsPasswordStrong(request.NewPassword))
            {
                throw new ValidationException("New password does not meet security requirements", new Dictionary<string, string[]>());
            }
            
            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            await _rateLimitService.ResetAttemptsAsync(rateKey);
            
            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Password has been successfully reset"
            };
        }

        public async Task LogoutAsync(string refreshToken, string accessToken)
        {
            var tokenHash = _refreshTokenService.HashToken(refreshToken);
            var user = await _context.Users.SingleOrDefaultAsync(u => u.RefreshTokenHash == tokenHash);

            if (user == null) return;

            user.RefreshTokenHash = null;
            user.RefreshTokenExpiryTime = DateTime.Now;
            await _context.SaveChangesAsync();

            var tokenExpiry = _jwtService.GetExpiryFromToken(accessToken);
            var ttl = tokenExpiry - DateTime.UtcNow;

            if (ttl.TotalSeconds > 0)
            {
                await _redis.StringSetAsync($"blacklist:{accessToken}", "true", ttl);
            }
        }

        public async Task<AuthResponse> ConfirmTwoFactorAsync(DTOs.TwoFactorRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email &&
            u.TwoFactorCode == request.Code &&
            u.TwoFactorCodeExpiryTime > DateTime.UtcNow);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid or expired 2FA code");

            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiryTime = null;
            //UpdateRefreshToken(user);

            await _context.SaveChangesAsync();

            var (accessToken, newRefreshToken) = _jwtService.GenerateTokens(user);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                Role = user.Role
            };
        }

        public async Task EnableTwoFactorAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            user.IsTwoFactorEnabled = true;
            await _context.SaveChangesAsync();
        }


        public async Task<AuthResponse> HandleExternalLoginAsync(string provider, string providerUserId, string email, string name)
        {
            // 1) ищем пользователя по email
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Role = "Client",
                    IsEmailConfirmed = true, // соц.логин => email уже подтверждён провайдером (на твоё усмотрение)
                                             // можно сохранить отображаемое имя, аватар и т.д.
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);
            user.RefreshTokenHash = _refreshTokenService.HashToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = user.Role,
                IsSuccess = true
            };

        }
    }
}
