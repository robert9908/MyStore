namespace AuthService.Configurations;

public class SecuritySettings
{
    public bool RequireHttps { get; set; } = true;
    public bool RequireEmailConfirmation { get; set; } = true;
    public PasswordPolicySettings PasswordPolicy { get; set; } = new();
    public AccountLockoutSettings AccountLockout { get; set; } = new();
}

public class PasswordPolicySettings
{
    public int RequiredLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
}

public class AccountLockoutSettings
{
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutTimeMinutes { get; set; } = 30;
}
