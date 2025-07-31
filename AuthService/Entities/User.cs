public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Client";
    public DateTime? RefreshTokenExpiryTime { get; set; } = DateTime.UtcNow;
    public string? RefreshTokenHash {  get; set; }

    public bool IsEmailConfirmed { get; set; } = false;
    public string EmailConfirmationToken { get; set; } = string.Empty;

    public string? PasswordResetToken {  get; set; } = string.Empty;
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public string? TwoFactorCode {  get; set; }
    public DateTime? TwoFactorCodeExpiryTime { get; set; }
    public bool IsTwoFactorEnabled { get; set; } = false;

    public bool IsBanned { get; set; } = false;
    public bool EmailConfirmed { get; set; } = false;
}