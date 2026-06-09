using ArenaApi.Hubs;
using ArenaApplication.IServices;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Services
{
    public class NotificationHubService : INotificationHub
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendToUserAsync(Guid userId, object notification, CancellationToken cancellationToken = default) =>
            _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", notification, cancellationToken);
    }
}
