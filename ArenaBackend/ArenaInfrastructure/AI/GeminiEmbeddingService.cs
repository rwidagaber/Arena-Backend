using ArenaApplication.IServices;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _client;
        private readonly GeminiSettings _settings;

        public GeminiEmbeddingService(
            HttpClient client,
            IOptions<GeminiSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var url = $"{_settings.BaseUrl}/text-embedding-004:embedContent?key={_settings.ApiKey}";

            var body = new
            {
                model = "models/text-embedding-004",
                content = new
                {
                    parts = new[] { new { text } }
                }
            };

            var response = await _client.PostAsJsonAsync(url, body);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Gemini Embedding Error: {responseText}");
                return [];
            }

            var json = JsonSerializer.Deserialize<JsonElement>(responseText);

            return json
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();
        }

        public async Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts)
        {
            var results = new List<float[]>();

            // ✅ Gemini free tier has rate limits, so we space requests slightly
            foreach (var text in texts)
            {
                var embedding = await GetEmbeddingAsync(text);
                results.Add(embedding);
                await Task.Delay(200); // avoid rate limit
            }

            return results;
        }
    }
}