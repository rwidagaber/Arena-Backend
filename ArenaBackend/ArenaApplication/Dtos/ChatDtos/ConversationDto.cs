using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Dtos.ChatDtos
{
    public class ConversationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int MessageCount { get; set; }
        public string LastMessage { get; set; } = string.Empty;
    }
}
