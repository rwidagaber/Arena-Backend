using ArenaApplication.IServices;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArenaMVC.Services
{
    // Minimal no-op implementation of INotificationHub for the MVC admin app.
    public class NoopNotificationHub : INotificationHub
    {
        public Task SendToUserAsync(Guid userId, object notification, CancellationToken cancellationToken = default)
        {
            // Intentionally do nothing for the MVC admin surface.
            return Task.CompletedTask;
        }
    }
}
