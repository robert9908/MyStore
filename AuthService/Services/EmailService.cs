using AuthService.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;

namespace AuthService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendConfirmationEmailAsync(string toEmail, string token)
        {
            var fromEmail = _config["Email:From"];
            var smtpHost = _config["Email:SmtpHost"];
            var smtpPort = int.Parse(_config["Email:SmtpPort"]);
            var smtpUser = _config["Email:Username"];
            var smtpPass = _config["Email:Password"];

            var link = $"https://yourdomain.com/api/auth/verify?token={token}";

            var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = "Email Confirmation",
                Body = $"Please confirm your email by clicking the following link:\n{link}",
                IsBodyHtml = false
            };

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetToken)
        {
            var resetLink = $"www.com";
            var subject = "Reset your password";
            var body = "Click the link to reset your password {resetLink}";

            //await SendMailAsync(to, subject, body);
        }
    }
}
