using ArenaDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.NotificationDtos
{
    public class CreateNotificationDto
    {
        public Guid MemberProfileId { get; set; }
        
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }
    }
}
