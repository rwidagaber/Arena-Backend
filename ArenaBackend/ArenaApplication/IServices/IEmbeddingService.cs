namespace ArenaApplication.IServices
{
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text);
        Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts);
    }
}