// ArenaDomain/Entities/Notifications/PushSubscription.cs
using ArenaDomain.Shared;

namespace ArenaDomain.Entities.Notifications
{
    public class PushSubscription : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}