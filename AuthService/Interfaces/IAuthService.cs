using AuthService.DTOs;

namespace AuthService.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ip);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);

    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, string ip);

    Task LogoutAsync(string refreshToken, string accessToken);
    Task<AuthResponse> HandleExternalLoginAsync(string provider, string providerUserId, string email, string name);
    
    Task<AuthResponse> ConfirmTwoFactorAsync(TwoFactorRequest request);
    Task EnableTwoFactorAsync(Guid userId);
}
