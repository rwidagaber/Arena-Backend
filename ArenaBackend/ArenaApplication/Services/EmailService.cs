using ArenaApplication.IServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace ArenaApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ── Core (private) ────────────────────────────────────────────────────

        private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var host = _configuration["EmailSettings:Host"]!;
            var port = int.Parse(_configuration["EmailSettings:Port"]!);
            var username = _configuration["EmailSettings:Username"]!;
            var password = _configuration["EmailSettings:Password"]!;
            var fromName = _configuration["EmailSettings:FromName"]!;
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]!);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var smtp = new SmtpClient();

            if (enableSsl)
                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
            else
                await smtp.ConnectAsync(host, port, SecureSocketOptions.None, cancellationToken);

            if (!string.IsNullOrEmpty(username))
                await smtp.AuthenticateAsync(username, password, cancellationToken);

            await smtp.SendAsync(message, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
        }

        // ── Authentication ────────────────────────────────────────────────────

        public Task SendOtpAsync(string toEmail, string otp, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, "Your Arena Verification Code", $"""
                <h2>Verification Code</h2>
                <p>Your OTP code is:</p>
                <h1 style="letter-spacing:8px">{otp}</h1>
                <p>This code expires in 10 minutes. Do not share it with anyone.</p>
            """, cancellationToken);

        // ── Subscriptions & Payments ──────────────────────────────────────────

        public Task SendPaymentConfirmedAsync(string toEmail, string firstName, decimal amount, string planName, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, "Payment Confirmed ✅", $"""
                <h2>Hey {firstName}!</h2>
                <p>Your payment of <strong>{amount:C}</strong> for the <strong>{planName}</strong> plan was successful.</p>
                <p>Your subscription is now active. Enjoy Arena!</p>
            """, cancellationToken);

        public Task SendSubscriptionExpiringAsync(string toEmail, string firstName, int daysLeft, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, "Your Subscription is Expiring Soon ⚠️", $"""
                <h2>Hey {firstName}!</h2>
                <p>Your Arena subscription expires in <strong>{daysLeft} day(s)</strong>.</p>
                <p>Renew now to keep access to all features and AI tools.</p>
            """, cancellationToken);

        public Task SendSubscriptionExpiredAsync(string toEmail, string firstName, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, "Your Subscription Has Expired ❌", $"""
                <h2>Hey {firstName}!</h2>
                <p>Your Arena subscription has expired.</p>
                <p>Renew your plan to continue booking sessions and using AI features.</p>
            """, cancellationToken);
    }
}