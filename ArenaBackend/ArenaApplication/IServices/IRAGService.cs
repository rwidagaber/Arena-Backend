using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.IServices
{
    public interface IRAGService
    {
        Task IndexKnowledgeBaseAsync();
        Task<string> SearchAsync(string query, int topK = 5);
        Task IndexMemberDataAsync(Guid memberProfileId);
    }
}
