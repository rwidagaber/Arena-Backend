using ArenaApi.Configurations;
using ArenaApplication.IServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using ArenaDomain.Shared;
using Microsoft.Extensions.Localization;
using MimeKit;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
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
        <span style='font-size:32px; font-weight:700; color:#1a1a1a; margin:0 6px; display:inline-block;'>{c}</span>"));
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



        public Task SendPasswordResetTokenAsync(string toEmail, string resetToken, string userEmail, CancellationToken cancellationToken = default)
        {
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
            var encodedEmail = Uri.EscapeDataString(userEmail);
            var resetLink = $"{_emailSettings.FrontendUrl}/reset-password?token={encodedToken}&email={encodedEmail}";

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
              ARENA <span style='color:#4DA352;'>GYM</span>
            </span>
          </td>
        </tr>
        <tr>
          <td style='padding:36px 32px;text-align:center;'>
            <h2 style='margin:0 0 8px;font-size:20px;color:#1a1a1a;font-weight:600;'>
              Reset your password
            </h2>
            <p style='margin:0 0 24px;font-size:14px;color:#6b6b6b;line-height:1.6;'>
              Click the button below to reset your password.
              Valid for <strong>10 minutes</strong> only.
            </p>
            <a href='{resetLink}'
               style='display:inline-block;padding:14px 32px;background:#4DA352;
                      color:#fff;border-radius:8px;text-decoration:none;
                      font-size:15px;font-weight:600;'>
              Reset Password
            </a>
            <div style='background:#fff7ed;border:0.5px solid #fed7aa;border-radius:8px;
                        padding:10px 16px;font-size:13px;color:#9a3412;
                        text-align:center;margin-top:24px;'>
              Expires in 10 minutes
            </div>
            <p style='margin-top:24px;font-size:13px;color:#9b9b9b;'>
              If you didn't request this, you can safely ignore this email.
            </p>
          </td>
        </tr>
        <tr>
          <td style='padding:20px 32px;background:#fafaf9;
                     border-top:0.5px solid #e0dfd8;text-align:center;
                     border-radius:0 0 16px 16px;'>
            <p style='margin:0;font-size:12px;color:#9b9b9b;'>
              Arena Gym · This is an automated email, please do not reply<br/>
              © 2026 Arena Gym. All rights reserved.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            return SendAsync(toEmail, "Reset Your Arena Password", body, cancellationToken);
        }
        // ── Subscriptions & Payments ──────────────────────────────────────────

        public Task SendPaymentConfirmedAsync(string toEmail, string firstName, decimal amount, string planName, CancellationToken cancellationToken = default)
        {
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
              ARENA <span style='color:#4DA352;'>GYM</span>
            </span>
          </td>
        </tr>
        <tr>
          <td style='padding:36px 32px;text-align:center;'>
            <h2 style='margin:0 0 8px;font-size:20px;color:#1a1a1a;font-weight:600;'>
              Payment Confirmed ✅
            </h2>
            <p style='margin:0 0 24px;font-size:14px;color:#6b6b6b;line-height:1.6;'>
              Hi <strong>{firstName}</strong>, your payment has been successfully processed.
            </p>
            <div style='background:#f0fdf4;border:0.5px solid #86efac;border-radius:8px;
                        padding:16px;text-align:center;margin-bottom:24px;'>
              <p style='margin:0 0 8px;font-size:13px;color:#6b6b6b;'>Amount Paid</p>
              <p style='margin:0;font-size:28px;font-weight:700;color:#166534;'>{amount:C}</p>
              <p style='margin:8px 0 0;font-size:14px;color:#166534;'>Plan: <strong>{planName}</strong></p>
            </div>
            <p style='margin:0;font-size:13px;color:#9b9b9b;'>
              Thank you for choosing Arena Gym. Enjoy your training!
            </p>
          </td>
        </tr>
        <tr>
          <td style='padding:20px 32px;background:#fafaf9;
                     border-top:0.5px solid #e0dfd8;text-align:center;
                     border-radius:0 0 16px 16px;'>
            <p style='margin:0;font-size:12px;color:#9b9b9b;'>
              Arena Gym · This is an automated email, please do not reply<br/>
              © 2026 Arena Gym. All rights reserved.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            return SendAsync(toEmail, "Payment Confirmed - Arena Gym ✅", body, cancellationToken);
        }

        public Task SendSubscriptionExpiringAsync(string toEmail, string firstName, int daysLeft, CancellationToken cancellationToken = default)
        {
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
              ARENA <span style='color:#4DA352;'>GYM</span>
            </span>
          </td>
        </tr>
        <tr>
          <td style='padding:36px 32px;text-align:center;'>
            <h2 style='margin:0 0 8px;font-size:20px;color:#1a1a1a;font-weight:600;'>
              Subscription Expiring Soon ⚠️
            </h2>
            <p style='margin:0 0 24px;font-size:14px;color:#6b6b6b;line-height:1.6;'>
              Hi <strong>{firstName}</strong>, your subscription is expiring soon.
            </p>
            <div style='background:#fff7ed;border:0.5px solid #fed7aa;border-radius:8px;
                        padding:16px;text-align:center;margin-bottom:24px;'>
              <p style='margin:0;font-size:28px;font-weight:700;color:#9a3412;'>{daysLeft} Days Left</p>
              <p style='margin:8px 0 0;font-size:13px;color:#9a3412;'>Renew now to keep your access</p>
            </div>
            <p style='margin:0;font-size:13px;color:#9b9b9b;'>
              Don't let your progress stop — renew your membership today!
            </p>
          </td>
        </tr>
        <tr>
          <td style='padding:20px 32px;background:#fafaf9;
                     border-top:0.5px solid #e0dfd8;text-align:center;
                     border-radius:0 0 16px 16px;'>
            <p style='margin:0;font-size:12px;color:#9b9b9b;'>
              Arena Gym · This is an automated email, please do not reply<br/>
              © 2026 Arena Gym. All rights reserved.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            return SendAsync(toEmail, "Your Subscription is Expiring Soon ⚠️", body, cancellationToken);
        }

        public Task SendSubscriptionExpiredAsync(string toEmail, string firstName, CancellationToken cancellationToken = default)
        {
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
              ARENA <span style='color:#4DA352;'>GYM</span>
            </span>
          </td>
        </tr>
        <tr>
          <td style='padding:36px 32px;text-align:center;'>
            <h2 style='margin:0 0 8px;font-size:20px;color:#1a1a1a;font-weight:600;'>
              Subscription Expired ❌
            </h2>
            <p style='margin:0 0 24px;font-size:14px;color:#6b6b6b;line-height:1.6;'>
              Hi <strong>{firstName}</strong>, your Arena Gym subscription has expired.
            </p>
            <div style='background:#fef2f2;border:0.5px solid #fca5a5;border-radius:8px;
                        padding:16px;text-align:center;margin-bottom:24px;'>
              <p style='margin:0;font-size:16px;font-weight:600;color:#991b1b;'>
                Your access has been suspended
              </p>
              <p style='margin:8px 0 0;font-size:13px;color:#991b1b;'>
                Renew your membership to continue training
              </p>
            </div>
            <p style='margin:0;font-size:13px;color:#9b9b9b;'>
              We'd love to have you back — renew today and pick up where you left off!
            </p>
          </td>
        </tr>
        <tr>
          <td style='padding:20px 32px;background:#fafaf9;
                     border-top:0.5px solid #e0dfd8;text-align:center;
                     border-radius:0 0 16px 16px;'>
            <p style='margin:0;font-size:12px;color:#9b9b9b;'>
              Arena Gym · This is an automated email, please do not reply<br/>
              © 2026 Arena Gym. All rights reserved.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            return SendAsync(toEmail, "Your Subscription Has Expired ❌", body, cancellationToken);
        }
        // ── Bookings ──────────────────────────────────────────────────────────

        public Task SendSessionReminderAsync(string toEmail, string firstName, DateTime bookingDate, CancellationToken cancellationToken = default)
        {
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
              ARENA <span style='color:#4DA352;'>GYM</span>
            </span>
          </td>
        </tr>
        <tr>
          <td style='padding:36px 32px;text-align:center;'>
            <h2 style='margin:0 0 8px;font-size:20px;color:#1a1a1a;font-weight:600;'>
              Session Reminder 💪
            </h2>
            <p style='margin:0 0 24px;font-size:14px;color:#6b6b6b;line-height:1.6;'>
              Hi <strong>{firstName}</strong>, just a reminder that you have a session booked for tomorrow.
            </p>
            <div style='background:#f0fdf4;border:0.5px solid #86efac;border-radius:8px;
                        padding:16px;font-size:18px;font-weight:700;color:#166534;
                        text-align:center;margin-bottom:24px;'>
              📅 {bookingDate:dddd, MMMM dd} at {bookingDate:hh:mm tt}
            </div>
            <p style='margin:0;font-size:13px;color:#9b9b9b;'>
              Make sure you're ready. See you there!
            </p>
          </td>
        </tr>
        <tr>
          <td style='padding:20px 32px;background:#fafaf9;
                     border-top:0.5px solid #e0dfd8;text-align:center;
                     border-radius:0 0 16px 16px;'>
            <p style='margin:0;font-size:12px;color:#9b9b9b;'>
              Arena Gym · This is an automated email, please do not reply<br/>
              © 2026 Arena Gym. All rights reserved.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            return SendAsync(toEmail, "Reminder: Your Session is Tomorrow! 🏋️", body, cancellationToken);
        }
        public Task SendSessionsExpiringSoonAsync(string toEmail, string firstName, int remainingSessions, CancellationToken cancellationToken = default)
        {
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
              ARENA <span style='color:#4DA352;'>GYM</span>
            </span>
          </td>
        </tr>
        <tr>
          <td style='padding:36px 32px;text-align:center;'>
            <h2 style='margin:0 0 8px;font-size:20px;color:#1a1a1a;font-weight:600;'>
              Sessions Running Low ⚠️
            </h2>
            <p style='margin:0 0 24px;font-size:14px;color:#6b6b6b;line-height:1.6;'>
              Hi <strong>{firstName}</strong>, you're almost out of sessions on your current plan.
            </p>
            <div style='background:#fff7ed;border:0.5px solid #fed7aa;border-radius:8px;
                        padding:16px;text-align:center;margin-bottom:24px;'>
              <p style='margin:0;font-size:28px;font-weight:700;color:#9a3412;'>{remainingSessions} Sessions Left</p>
              <p style='margin:8px 0 0;font-size:13px;color:#9a3412;'>Renew or top up to keep training</p>
            </div>
            <p style='margin:0;font-size:13px;color:#9b9b9b;'>
              Don't let your progress stop — renew your plan today!
            </p>
          </td>
        </tr>
        <tr>
          <td style='padding:20px 32px;background:#fafaf9;
                     border-top:0.5px solid #e0dfd8;text-align:center;
                     border-radius:0 0 16px 16px;'>
            <p style='margin:0;font-size:12px;color:#9b9b9b;'>
              Arena Gym · This is an automated email, please do not reply<br/>
              © 2026 Arena Gym. All rights reserved.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            return SendAsync(toEmail, "Your Sessions Are Running Low ⚠️", body, cancellationToken);
        }
    }

}