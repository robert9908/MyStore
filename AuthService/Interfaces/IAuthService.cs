using AuthService.DTOs;
using Microsoft.AspNetCore.Identity.Data;
using System.Runtime.CompilerServices;

namespace AuthService.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(DTOs.RegisterRequest request);
        Task<AuthResponse> LoginAsync(DTOs.LoginRequest request, string ip);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);

        Task ForgotPasswordAsync(DTOs.ForgotPasswordRequest request);
        Task ResetPasswordAsync(DTOs.ResetPasswordRequest request, string ip);

        Task LogoutAsync(string refreshToken, string accessToken);
    }
}
