using ArenaApplication.AI.ArenaApplication.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IBookingAIService
    {
        Task<string> HandleBookingRequestAsync(
            Guid memberProfileId,
            IntentResult intent,
            string userMessage,
            string memberName = "Member");
    }
}
