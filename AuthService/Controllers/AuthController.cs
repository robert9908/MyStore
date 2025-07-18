using AuthService.DTOs;
using AuthService.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Validations;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController: ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public AuthController(IAuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(DTOs.RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(DTOs.LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);
            return Ok(result);
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

            if (user is null)
                return BadRequest("Invalid or expired token");

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = string.Empty;

            await _context.SaveChangesAsync();

            return Ok("Email confirmed succesfully");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok("If an account with this email exists, a reset link hsa been sent");

        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] DTOs.ResetPasswordRequest request)
        {
            await _authService.ResetPasswordAsync(request);
            return Ok("Password has been successfully reset");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer", "");

            await _authService.LogoutAsync(request.RefreshToken, accessToken);

            return Ok(new { message = "Logout successful" });
        }
    }
}
