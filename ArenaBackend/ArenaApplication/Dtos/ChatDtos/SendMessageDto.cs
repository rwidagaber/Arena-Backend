using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ArenaApplication.Dtos.ChatDtos
{
    public class SendMessageDto
    {
        [Required]
        public Guid MemberProfileId { get; set; }
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
