using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class MemberHealthRAGService : IMemberHealthRAGService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IGeminiCompletionService _gemini;
        private readonly AppDbContext _context;

        public MemberHealthRAGService(
            IEmbeddingService embeddingService,
            IGeminiCompletionService gemini,
            AppDbContext context)
        {
            _embeddingService = embeddingService;
            _gemini = gemini;
            _context = context;
        }

        public async Task SaveHealthInfoAsync(Guid memberProfileId, string content, string category)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var exists = await _context.MemberHealthVectors
                .AnyAsync(v => v.MemberProfileId == memberProfileId
                            && v.Content == content
                            && v.IsActive);

            if (exists)
                return;

            var embedding = await _embeddingService.GetEmbeddingAsync(content);
            if (embedding.Length == 0)
                Console.WriteLine($"Saving health memory without embedding: {content}");

            _context.MemberHealthVectors.Add(new MemberHealthVector
            {
                MemberProfileId = memberProfileId,
                Content = content,
                Category = string.IsNullOrWhiteSpace(category) ? "ChatMention" : category,
                EmbeddingJson = JsonSerializer.Serialize(embedding),
                RecordedAt = DateTime.UtcNow,
                IsActive = true
            });

            await _context.SaveChangesAsync();
        }

        public async Task SyncProfileHealthDataAsync(Guid memberProfileId)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                return;

            var oldProfileVectors = await _context.MemberHealthVectors
                .Where(v => v.MemberProfileId == profile.Id && v.Category == "ProfileSync")
                .ToListAsync();

            foreach (var old in oldProfileVectors)
                old.IsActive = false;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(profile.Injuries))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following injury: {profile.Injuries}", "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.HealthConditions))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following health condition: {profile.HealthConditions}", "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.DietaryRestrictions))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following dietary restriction: {profile.DietaryRestrictions}", "ProfileSync");
        }

        public async Task ExtractAndSaveFromChatAsync(Guid memberProfileId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return;

            var extractPrompt = $$"""
You are a health information extractor for a gym app.
Analyze this user message and determine if it mentions any injury, pain, disease,
diagnosed health condition, illness, allergy, medication, dietary restriction,
pregnancy, surgery, or physical limitation that should be remembered for future AI plans.

Message: "{{userMessage}}"

Return ONLY valid JSON, no markdown:
{
  "hasHealthInfo": true or false,
  "items": [
    {
      "extractedInfo": "clear English sentence describing one health fact",
      "category": "Injury" or "Disease" or "HealthCondition" or "Allergy" or "Medication" or "DietaryRestriction" or "PhysicalLimitation"
    }
  ]
}

Examples:
"I have knee pain and diabetes"
=> {"hasHealthInfo":true,"items":[{"extractedInfo":"Member has knee pain","category":"Injury"},{"extractedInfo":"Member has diabetes","category":"Disease"}]}

"عندي ألم في الركبة من امبارح"
=> {"hasHealthInfo":true,"items":[{"extractedInfo":"Member reported knee pain starting recently","category":"Injury"}]}

"what should I eat before training"
=> {"hasHealthInfo":false,"items":[]}

Only flag actual member health facts, not general questions.
""";

            try
            {
                var response = await _gemini.GetCompletionAsync(
                    extractPrompt,
                    new List<ChatMessageDto>(),
                    userMessage);

                var clean = CleanJson(response);
                var result = JsonSerializer.Deserialize<HealthExtractResult>(
                    clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.HasHealthInfo != true)
                    return;

                var items = result.Items.Any()
                    ? result.Items
                    : string.IsNullOrWhiteSpace(result.ExtractedInfo)
                        ? []
                        :
                        [
                            new HealthExtractItem
                            {
                                ExtractedInfo = result.ExtractedInfo,
                                Category = result.Category
                            }
                        ];

                foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.ExtractedInfo)))
                    await SaveHealthInfoAsync(memberProfileId, item.ExtractedInfo, item.Category);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health extraction failed: {ex.Message}");
            }
        }

        public async Task<string> GetRelevantHealthContextAsync(Guid memberProfileId, string query, int topK = 5)
        {
            var vectors = await _context.MemberHealthVectors
                .Where(v => v.MemberProfileId == memberProfileId && v.IsActive)
                .ToListAsync();

            if (!vectors.Any())
                return string.Empty;

            var critical = vectors
                .Where(v => IsCriticalCategory(v.Category))
                .Select(v => v.Content)
                .Distinct();

            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);
            if (queryEmbedding.Length == 0)
            {
                var criticalOnly = critical.ToList();
                return criticalOnly.Any()
                    ? string.Join("\n", criticalOnly.Select(c => $"- {c}"))
                    : string.Empty;
            }

            var scored = vectors
                .Select(v => new
                {
                    v.Content,
                    Score = CosineSimilarity(
                        queryEmbedding,
                        JsonSerializer.Deserialize<float[]>(v.EmbeddingJson) ?? [])
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            var relevant = scored
                .Where(s => s.Score > 0.45)
                .Select(s => s.Content);

            var combined = critical.Union(relevant).Distinct().ToList();
            return combined.Any()
                ? string.Join("\n", combined.Select(c => $"- {c}"))
                : string.Empty;
        }

        private static bool IsCriticalCategory(string category) =>
            category is "Injury" or "Disease" or "HealthCondition" or "Allergy" or "Medication"
                or "DietaryRestriction" or "PhysicalLimitation" or "ProfileSync";

        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length || a.Length == 0)
                return 0;

            double dot = 0, magA = 0, magB = 0;
            for (var i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            if (magA == 0 || magB == 0)
                return 0;

            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        private static string CleanJson(string raw)
        {
            var clean = raw.Trim();
            if (clean.StartsWith("```json")) clean = clean[7..];
            else if (clean.StartsWith("```")) clean = clean[3..];
            if (clean.EndsWith("```")) clean = clean[..^3];

            var start = clean.IndexOf('{');
            var end = clean.LastIndexOf('}');
            if (start >= 0 && end > start)
                clean = clean.Substring(start, end - start + 1);

            return clean.Trim();
        }
    }

    public class HealthExtractResult
    {
        public bool HasHealthInfo { get; set; }
        public List<HealthExtractItem> Items { get; set; } = [];
        public string ExtractedInfo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class HealthExtractItem
    {
        public string ExtractedInfo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
