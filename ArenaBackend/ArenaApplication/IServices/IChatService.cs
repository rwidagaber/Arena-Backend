using ArenaApplication.Dtos.ChatDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IChatService
    {
        Task<string> SendMessageAsync(Guid memberProfileId, string userMessage);
        Task<List<ChatMessageDto>> GetHistoryAsync(Guid memberProfileId);
    }
}
