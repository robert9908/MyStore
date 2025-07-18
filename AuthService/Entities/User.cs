public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Client";
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiryTime { get; set; } = DateTime.UtcNow;

    public bool IsEmailConfirmed { get; set; } = false;
    public string EmailConfirmationToken { get; set; } = string.Empty;

    public string? PasswordResetToken {  get; set; } = string.Empty;
    public DateTime? PasswordResetTokenExpiry { get; set; }
}