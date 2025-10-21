using BCrypt.Net;

namespace AuthService.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    bool IsPasswordStrong(string password);
}

public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12; // BCrypt work factor for security

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // At least 8 characters, contains uppercase, lowercase, digit, and special character
        return password.Length >= 8 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));
    }
}
