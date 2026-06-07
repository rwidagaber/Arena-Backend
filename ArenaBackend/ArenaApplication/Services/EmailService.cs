using ArenaApi.Configurations;
using ArenaApplication.IServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;
using MimeKit;

namespace ArenaApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IStringLocalizer<ArenaLocalization> _localizer;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IStringLocalizer<ArenaLocalization> localizer)
        {
            _emailSettings = emailSettings.Value;
            _localizer = localizer;
        }

        // ── Core (private) ────────────────────────────────────────────────────

        private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var port = _emailSettings.Port;
            var username = _emailSettings.Username;
            var password = _emailSettings.Password;
            var host = _emailSettings.SmtpServer;
            var fromName = _emailSettings.SenderName;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.Auto, cancellationToken);
            await smtp.AuthenticateAsync(username, password, cancellationToken);
            await smtp.SendAsync(message, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
        }

        // ── Authentication ────────────────────────────────────────────────────

        public Task SendOtpAsync(string toEmail, string otp, CancellationToken cancellationToken = default)
        {
            var digits = string.Join("", otp.Select(c => $@"
        <div style='width:52px;height:64px;background:#fafaf8;border:1.5px solid #e0dfd8;
                    border-radius:10px;display:inline-block;text-align:center;line-height:64px;
                    font-size:28px;font-weight:700;color:#1a1a1a;margin:0 5px;'>{c}</div>"));

            var body = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f4f4f0;font-family:Helvetica Neue,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr><td align='center' style='padding:32px 16px;'>
      <table width='480' cellpadding='0' cellspacing='0'
             style='background:#fff;border-radius:16px;border:0.5px solid #e0dfd8;'>

        <tr>
          <td style='background:#1a1a1a;padding:32px;text-align:center;border-radius:16px 16px 0 0;'>
            <span style='color:#fff;font-size:22px;font-weight:600;letter-spacing:1px;'>
              ARENA
              <span style='color:#4DA352;text-shadow:0 0 8px rgba(77,163,82,0.6);'>
                GYM
              </span>
            </span>
          </td>
        </tr>

        <tr>
          <td style='padding:36px 32px;'>
            <h2 style='margin:0 0 8px;font-size:20px;color:#1a1a1a;font-weight:600;'>
              Confirm your email
            </h2>
            <p style='margin:0 0 24px;font-size:14px;color:#6b6b6b;line-height:1.6;'>
              Use the code below to verify your account.
              Valid for <strong>10 minutes</strong> only.
            </p>
            <div style='text-align:center;margin:28px 0;'>{digits}</div>
            <div style='background:#fff7ed;border:0.5px solid #fed7aa;border-radius:8px;
                        padding:10px 16px;font-size:13px;color:#9a3412;
                        text-align:center;margin-bottom:24px;'>
              Expires in 10 minutes
            </div>
            <p style='margin:0;font-size:13px;color:#9b9b9b;'>
              If you didn't request this, you can safely ignore this email.
            </p>
          </td>
        </tr>

        <tr>
          <td style='padding:20px 32px;background:#fafaf9;
                     border-top:0.5px solid #e0dfd8;text-align:center;
                     border-radius:0 0 16px 16px;'>
            <p style='margin:0;font-size:12px;color:#9b9b9b;line-height:1.6;'>
              Arena Gym &nbsp;·&nbsp; This is an automated email, please do not reply<br/>
              © 2026 Arena Gym. All rights reserved.
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            return SendAsync(toEmail, "Your Arena Verification Code", body, cancellationToken);
        }

        // ── Subscriptions & Payments ──────────────────────────────────────────

        public Task SendPaymentConfirmedAsync(string toEmail, string firstName, decimal amount, string planName, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, _localizer["EmailPaymentConfirmedSubject"], string.Format(_localizer["EmailPaymentConfirmedBody"], firstName, amount, planName), cancellationToken);

        public Task SendSubscriptionExpiringAsync(string toEmail, string firstName, int daysLeft, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, _localizer["EmailSubscriptionExpiringSubject"], string.Format(_localizer["EmailSubscriptionExpiringBody"], firstName, daysLeft), cancellationToken);

        public Task SendSubscriptionExpiredAsync(string toEmail, string firstName, CancellationToken cancellationToken = default) =>
            SendAsync(toEmail, _localizer["EmailSubscriptionExpiredSubject"], string.Format(_localizer["EmailSubscriptionExpiredBody"], firstName), cancellationToken);
    }
}