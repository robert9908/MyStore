namespace AuthService.Configurations;

public class RateLimitSettings
{
    public LoginRateLimit Login { get; set; } = new();
    public RegistrationRateLimit Registration { get; set; } = new();
    public PasswordResetRateLimit PasswordReset { get; set; } = new();
}

public class LoginRateLimit
{
    public int MaxAttempts { get; set; } = 5;
    public int WindowMinutes { get; set; } = 15;
    public int LockoutMinutes { get; set; } = 30;
}

public class RegistrationRateLimit
{
    public int MaxAttempts { get; set; } = 3;
    public int WindowMinutes { get; set; } = 60;
}

public class PasswordResetRateLimit
{
    public int MaxAttempts { get; set; } = 3;
    public int WindowMinutes { get; set; } = 60;
}
