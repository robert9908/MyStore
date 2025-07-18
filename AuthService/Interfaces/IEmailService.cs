using System.Runtime.CompilerServices;

namespace AuthService.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string toEmail, string token);
        Task SendPasswordResetEmailAsync(string to, string resetToken);

    }
}
