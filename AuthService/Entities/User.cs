using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(320)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("role")]
    public string Role { get; set; } = "Client";

    [Column("refresh_token_hash")]
    public string? RefreshTokenHash { get; set; }

    [Column("refresh_token_expiry_time")]
    public DateTime? RefreshTokenExpiryTime { get; set; }

    [Column("is_email_confirmed")]
    public bool IsEmailConfirmed { get; set; } = false;

    [MaxLength(255)]
    [Column("email_confirmation_token")]
    public string? EmailConfirmationToken { get; set; }

    [MaxLength(255)]
    [Column("password_reset_token")]
    public string? PasswordResetToken { get; set; }

    [Column("password_reset_token_expiry")]
    public DateTime? PasswordResetTokenExpiry { get; set; }

    [MaxLength(10)]
    [Column("two_factor_code")]
    public string? TwoFactorCode { get; set; }

    [Column("two_factor_code_expiry_time")]
    public DateTime? TwoFactorCodeExpiryTime { get; set; }

    [Column("is_two_factor_enabled")]
    public bool IsTwoFactorEnabled { get; set; } = false;

    [Column("is_banned")]
    public bool IsBanned { get; set; } = false;

    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    [Column("last_failed_login")]
    public DateTime? LastFailedLogin { get; set; }

    [Column("lockout_end")]
    public DateTime? LockoutEnd { get; set; }

    // OAuth fields
    [MaxLength(50)]
    [Column("provider")]
    public string? Provider { get; set; }

    [MaxLength(255)]
    [Column("provider_user_id")]
    public string? ProviderUserId { get; set; }

    // Audit fields
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [MaxLength(45)]
    [Column("last_login_ip")]
    public string? LastLoginIp { get; set; }

    // Navigation properties
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}