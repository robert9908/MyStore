using AuthService.Configurations;
using AuthService.Interfaces;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using MailKit.Net.Smtp;
using MailKit.Security;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace AuthService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IOptions<EmailSettings> _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings;
            _logger = logger;
        }
        public async Task SendConfirmationEmailAsync(string toEmail, string token)
        {
            var link = $"https://yourdomain.com/api/auth/verify?token={Uri.EscapeDataString(token)}";
            var subject = "Email Confirmation";
            var body = $"Please confirm your email by clicking the following link:\n{link}";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetToken)
        {
            var resetLink = $"https://yourdomain.com/reset-password?token={Uri.EscapeDataString(resetToken)}";
            var subject = "Reset your password";
            var body = $"Click the link to reset your password: {resetLink}";
            await SendEmailAsync(to, subject, body);
        }

        public async Task SendTwoFactorCodeAsync(string email, string code)
        {
            var subject = "Your 2FA Code";
            var body = $"Your login confirmation code is: {code}. It expires in 5 minutes.";
            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_emailSettings.Value.From));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                message.Body = new TextPart("plain")
                {
                    Text = body
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_emailSettings.Value.SmtpHost, _emailSettings.Value.SmtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSettings.Value.Username, _emailSettings.Value.Password);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                throw;
            }
        }
    }
}
