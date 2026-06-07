namespace ArenaApplication.IServices
{
    public interface IEmailService
    {
        // ── Authentication ────────────────────────────────────────────────────
        Task SendOtpAsync(string toEmail, string otp, CancellationToken cancellationToken = default);

        Task SendPasswordResetTokenAsync(string toEmail, string resetToken, string userEmail, CancellationToken cancellationToken = default);
        // ── Subscriptions & Payments ──────────────────────────────────────────
        Task SendPaymentConfirmedAsync(string toEmail, string firstName, decimal amount, string planName, CancellationToken cancellationToken = default);
        Task SendSubscriptionExpiringAsync(string toEmail, string firstName, int daysLeft, CancellationToken cancellationToken = default);
        Task SendSubscriptionExpiredAsync(string toEmail, string firstName, CancellationToken cancellationToken = default);

       
    }
}