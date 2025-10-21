using AuthService.DTOs;
using AuthService.Interfaces;
using AuthService.Data;
using AuthService.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Serilog;


namespace AuthService.Controllers
{
    /// <summary>
    /// Authentication controller providing user registration, login, and token management
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, AppDbContext context, ILogger<AuthController> logger)
        {
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>Registration result</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                _logger.LogInformation("User registered successfully: {Email}", request.Email);
                return CreatedAtAction(nameof(Register), result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogWarning("Registration failed - user already exists: {Email}", request.Email);
                return Conflict(new { error = new { code = "USER_EXISTS", message = "User already exists" } });
            }
        }
        /// <summary>
        /// Authenticate user and return access tokens
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication tokens</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var clientIp = GetClientIpAddress();
            var result = await _authService.LoginAsync(request, clientIp);
            
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(result);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return BadRequest(new
                {
                    error = new
                    {
                        code = "TOKEN_MISSING",
                        message = "Refresh token is required"
                    }
                });

            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Refresh token failed: {Message}", result.Message);
                return BadRequest(new
                {
                    error = new
                    {
                        code = "REFRESH_FAILED",
                        message = result.Message
                    }
                });
            }

            return Ok(result);
        }

        [HttpGet("verify")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new
                {
                    error = new
                    {
                        code = "TOKEN_MISSING",
                        message = "Token is required"
                    }
                });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

            if (user is null)
            {
                _logger.LogWarning("Email verification failed: invalid or expired token");
                return BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_TOKEN",
                        message = "Invalid or expired token"
                    }
                });
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = string.Empty;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verified for user {Email}", user.Email);
            return Ok(new { message = "Email confirmed successfully" });
        }

        /// <summary>
        /// Request password reset email
        /// </summary>
        /// <param name="request">Email for password reset</param>
        /// <returns>Reset request confirmation</returns>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request);
            
            _logger.LogInformation("Password reset email requested for {Email}", request.Email);
            return Ok(new { message = "If an account with this email exists, a reset link has been sent" });
        }

        /// <summary>
        /// Reset user password using reset token
        /// </summary>
        /// <param name="request">Password reset details</param>
        /// <returns>Reset result</returns>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var clientIp = GetClientIpAddress();
            var result = await _authService.ResetPasswordAsync(request, clientIp);
            
            _logger.LogInformation("Password reset successfully for token: {Token}", request.Token[..8] + "...");
            return Ok(result);
        }

        /// <summary>
        /// Logout user and invalidate tokens
        /// </summary>
        /// <param name="request">Logout request with refresh token</param>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer", "").Trim();

            if (string.IsNullOrEmpty(accessToken))
                return BadRequest(new { error = new { code = "TOKEN_MISSING", message = "Access token is missing" } });

            await _authService.LogoutAsync(request.RefreshToken, accessToken);

            _logger.LogInformation("User logged out successfully");
            return Ok(new { message = "Logout successful" });
        }

        [HttpGet("google/login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin([FromQuery] string? redirectUri = null)
        {
 
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback)),
                Items = { ["returnUrl"] = string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri }
            };

            return Challenge(props, "Google");
        }

        [HttpGet("google/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {

            var result = await HttpContext.AuthenticateAsync("External");

            if (!result.Succeeded || result.Principal is null)
            {
                _logger.LogWarning("Google OAuth failed: {Failure}", result.Failure?.Message);
                return BadRequest(new
                {
                    error = new { code = "OAUTH_FAILED", message = "Google authentication failed" }
                });
            }

            var principal = result.Principal;

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst("email")?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value
                        ?? principal.FindFirst("name")?.Value;
            var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {

                await HttpContext.SignOutAsync("External");
                return BadRequest(new
                {
                    error = new { code = "EMAIL_REQUIRED", message = "Email scope is required" }
                });
            }

            var oauthResult = await _authService.HandleExternalLoginAsync(
                provider: "Google",
                providerUserId: sub ?? "",
                email: email,
                name: name ?? email
            );

            await HttpContext.SignOutAsync("External");


            var returnUrl = result.Properties?.Items.TryGetValue("returnUrl", out var ru) == true ? ru : "/";
            var redirectWithTokens = $"{returnUrl}#access_token={Uri.EscapeDataString(oauthResult.AccessToken)}&refresh_token={Uri.EscapeDataString(oauthResult.RefreshToken)}&role={Uri.EscapeDataString(oauthResult.Role)}";

            return Redirect(redirectWithTokens);
        }

        [HttpGet("external/denied")]
        [AllowAnonymous]
        public IActionResult ExternalDenied()
        {
            return BadRequest(new { error = new { code = "OAUTH_DENIED", message = "External authentication denied" } });
        }

        /// <summary>
        /// Get client IP address from request
        /// </summary>
        /// <returns>Client IP address</returns>
        private string GetClientIpAddress()
        {
            var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
