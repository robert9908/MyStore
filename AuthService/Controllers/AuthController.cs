using AuthService.DTOs;
using AuthService.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController: ControllerBase
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

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] DTOs.RegisterRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}", request.Email, result.Message);
                return BadRequest(new
                {
                    error = new
                    {
                        code = "LOGIN_FAILED",
                        message = result.Message
                    }
                });
            }

            _logger.LogInformation("User logged in: {Email}", request.Email);
            return Ok(result);
        }
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] DTOs.LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}", request.Email, result.Message);
                return BadRequest(new
                {
                    error = new
                    {
                        code = "LOGIN_FAILED",
                        message = result.Message
                    }
                });
            }

            _logger.LogInformation("User logged in: {Email}", request.Email);
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

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] DTOs.ForgotPasswordRequest request)
        {

            await _authService.ForgotPasswordAsync(request);

            _logger.LogInformation("Password reset email requested for {Email}", request.Email);
            return Ok(new { message = "If an account with this email exists, a reset link has been sent" });
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ResetPassword([FromBody] DTOs.ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Reset password failed: {Message}", result.Message);
                return BadRequest(new 
                {
                    error = new 
                    {
                        code = "RESET_FAILED",
                        message = result.Message
                    }
                });
            }

            _logger.LogInformation("Password reset successfully for {Email}", request.Email);
            return Ok(new { message = "Password has been successfully reset" });
        }

        [HttpPost("logout")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer", "").Trim();

            if (string.IsNullOrEmpty(accessToken))
                return BadRequest(new
                {
                    error = new
                    {
                        code = "TOKEN_MISSING",
                        message = "Access token is missing"
                    }
                });

            await _authService.LogoutAsync(request.RefreshToken, accessToken);

            _logger.LogInformation("User logged out with refresh token: {RefreshToken}", request.RefreshToken);
            return Ok(new { message = "Logout successful" });
        }
    }
}
