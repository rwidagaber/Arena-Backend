using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ChatDtos
{
    public class CreateConversationDto
    {
        public Guid MemberProfileId { get; set; }
        public string Title { get; set; } = "New Chat";
    }
}
