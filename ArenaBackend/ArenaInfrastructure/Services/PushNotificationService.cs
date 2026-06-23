// ArenaInfrastructure/Services/PushNotificationService.cs
using ArenaApplication.IServices;
using ArenaDomain.Entities.Notifications;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using WebPush;

namespace ArenaInfrastructure.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly AppDbContext _context;
        private readonly VapidDetails _vapid;

        public PushNotificationService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _vapid = new VapidDetails(
                "mailto:admin@arena.com",
                configuration["Vapid:PublicKey"]!,
                configuration["Vapid:PrivateKey"]!);
        }

        public async Task SendAsync(Guid userId, string title, string message, string url = "/dashboard")
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!subscriptions.Any()) return;

            var payload = JsonSerializer.Serialize(new { title, message, url });
            var client = new WebPushClient();

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                    await client.SendNotificationAsync(pushSub, payload, _vapid);
                }
                catch
                {
                    // لو الـ subscription انتهت — احذفها
                    _context.PushSubscriptions.Remove(sub);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}