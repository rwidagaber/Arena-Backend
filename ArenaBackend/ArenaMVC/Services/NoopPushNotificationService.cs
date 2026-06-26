using ArenaApplication.IServices;
using System;
using System.Threading.Tasks;

namespace ArenaMVC.Services
{
    // Minimal no-op implementation of IPushNotificationService for the MVC admin app.
    public class NoopPushNotificationService : IPushNotificationService
    {
        public Task SendAsync(Guid userId, string title, string message, string url = "/dashboard")
        {
            // Intentionally do nothing for the MVC admin surface.
            return Task.CompletedTask;
        }
    }
}
