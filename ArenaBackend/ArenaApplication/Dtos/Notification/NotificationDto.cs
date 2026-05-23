using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.Notification
{
    internal class NotificationDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }
    }
}
