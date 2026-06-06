using ArenaApplication.IServices;
using ArenaDomain.Shared;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MimeKit;

namespace ArenaApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public EmailService(
            IConfiguration configuration,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _configuration = configuration;
            _localizer = localizer;
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
            SendAsync(toEmail, _localizer["EmailOtpSubject"], string.Format(_localizer["EmailOtpBody"], otp), cancellationToken);

        // ── Subscriptions & Payments ──────────────────────────────────────────

        public Task SendPaymentConfirmedAsync(string toEmail, string firstName, decimal amount, string planName, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, _localizer["EmailPaymentConfirmedSubject"], string.Format(_localizer["EmailPaymentConfirmedBody"], firstName, amount, planName), cancellationToken);

        public Task SendSubscriptionExpiringAsync(string toEmail, string firstName, int daysLeft, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, _localizer["EmailSubscriptionExpiringSubject"], string.Format(_localizer["EmailSubscriptionExpiringBody"], firstName, daysLeft), cancellationToken);

        public Task SendSubscriptionExpiredAsync(string toEmail, string firstName, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, _localizer["EmailSubscriptionExpiredSubject"], string.Format(_localizer["EmailSubscriptionExpiredBody"], firstName), cancellationToken);
    }
}