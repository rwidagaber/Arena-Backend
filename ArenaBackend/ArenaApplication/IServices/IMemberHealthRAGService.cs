namespace ArenaApplication.IServices
{
    public interface IMemberHealthRAGService
    {
        Task SaveHealthInfoAsync(
            Guid memberProfileId, string content, string category);

        Task SyncProfileHealthDataAsync(Guid memberProfileId);

        Task<string> GetRelevantHealthContextAsync(
            Guid memberProfileId, string query, int topK = 5);

        Task ExtractAndSaveFromChatAsync(
            Guid memberProfileId, string userMessage);
    }
}