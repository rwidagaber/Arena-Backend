using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ArenaApi.Hubs
{
    [Authorize]
    public class NotificationHub : Hub 
    {
    }
}
