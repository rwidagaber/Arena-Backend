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
        private readonly IGeminiCompletionService _geminiAI;
        private readonly AppDbContext _context;

        public MemberHealthRAGService(
            IEmbeddingService embeddingService,
            IGeminiCompletionService geminiAI,
            AppDbContext context)
        {
            _embeddingService = embeddingService;
            _geminiAI= geminiAI;
            _context = context;
        }

        // ✅ Save a single piece of health info with its embedding
        public async Task SaveHealthInfoAsync(
            Guid memberProfileId, string content, string category)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            // ✅ Avoid duplicate entries (same content already saved & active)
            var exists = await _context.MemberHealthVectors
                .AnyAsync(v => v.MemberProfileId == memberProfileId
                            && v.Content == content
                            && v.IsActive);

            if (exists) return;

            var embedding = await _embeddingService.GetEmbeddingAsync(content);

            if (embedding.Length == 0)
            {
                Console.WriteLine($"⚠️ Failed to embed health info, saving without vector: {content}");
            }

            _context.MemberHealthVectors.Add(new MemberHealthVector
            {
                MemberProfileId = memberProfileId,
                Content = content,
                Category = category,
                EmbeddingJson = JsonSerializer.Serialize(embedding),
                RecordedAt = DateTime.UtcNow,
                IsActive = true
            });

            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ Saved health vector [{category}] for member {memberProfileId}: {content}");
        }

        // ✅ Sync from MemberProfile fields (call this on profile update)
        public async Task SyncProfileHealthDataAsync(Guid memberProfileId)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null) return;

            // ✅ Deactivate old profile-sourced vectors (will be re-added if still present)
            var oldProfileVectors = await _context.MemberHealthVectors
                .Where(v => v.MemberProfileId == profile.Id
                         && v.Category == "ProfileSync")
                .ToListAsync();

            foreach (var old in oldProfileVectors)
                old.IsActive = false;

            await _context.SaveChangesAsync();

            // ✅ Re-add current profile data as fresh vectors
            if (!string.IsNullOrWhiteSpace(profile.Injuries))
                await SaveHealthInfoAsync(
                    profile.Id,
                    $"Member has the following injury: {profile.Injuries}",
                    "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.HealthConditions))
                await SaveHealthInfoAsync(
                    profile.Id,
                    $"Member has the following health condition: {profile.HealthConditions}",
                    "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.DietaryRestrictions))
                await SaveHealthInfoAsync(
                    profile.Id,
                    $"Member has the following dietary restriction: {profile.DietaryRestrictions}",
                    "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.Goal))
                await SaveHealthInfoAsync(
                    profile.Id,
                    $"Member's fitness goal is: {profile.Goal}",
                    "ProfileSync");

            Console.WriteLine($"✅ Synced profile health data for {profile.FirstName}");
        }

        // ✅ Extract health-relevant info from a chat message using AI, then save
        public async Task ExtractAndSaveFromChatAsync(
            Guid memberProfileId, string userMessage)
        {
            var extractPrompt = $$"""
    You are a health information extractor for a gym app.
    Analyze this user message and determine if it mentions
    ANY injury, pain, disease, diagnosed health condition, illness,
    allergy, medication, dietary restriction, pregnancy, surgery,
    or physical limitation that should be remembered for future use.

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
    "عندي ألم في الركبة من امبارح" 
    => {"hasHealthInfo":true,"items":[{"extractedInfo":"Member reported knee pain starting recently","category":"Injury"}]}

    "اكتشفت إني عندي سكر"
    => {"hasHealthInfo":true,"items":[{"extractedInfo":"Member has diabetes","category":"Disease"}]}

    "I have knee pain and I am allergic to peanuts"
    => {"hasHealthInfo":true,"items":[{"extractedInfo":"Member has knee pain","category":"Injury"},{"extractedInfo":"Member is allergic to peanuts","category":"Allergy"}]}

    "what should I eat before training"
    => {"hasHealthInfo":false,"items":[]}

    Only flag genuinely NEW health information, not general questions.
    """;

            try
            {
                var response = await _geminiAI.GetCompletionAsync(
                    extractPrompt, new List<ChatMessageDto>(), userMessage);

                var clean = CleanJson(response);
                var result = JsonSerializer.Deserialize<HealthExtractResult>(
                    clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.HasHealthInfo == true)
                {
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
                    {
                        await SaveHealthInfoAsync(
                            memberProfileId,
                            item.ExtractedInfo,
                            string.IsNullOrWhiteSpace(item.Category) ? "ChatMention" : item.Category);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Health extraction failed: {ex.Message}");
                // Fail silently — this is a background enhancement, not critical path
            }
        }

        // ✅ Search for relevant health context given a query (e.g. workout request)
        public async Task<string> GetRelevantHealthContextAsync(
            Guid memberProfileId, string query, int topK = 5)
        {
            var vectors = await _context.MemberHealthVectors
                .Where(v => v.MemberProfileId == memberProfileId && v.IsActive)
                .ToListAsync();

            if (!vectors.Any())
                return string.Empty;

            // ✅ Always include safety-critical health info regardless of similarity score
            // Safety-critical info should never be filtered out by relevance threshold
            var critical = vectors
                .Where(v => v.Category is "Injury" or "Disease" or "HealthCondition" or "Allergy" or "Medication" or "DietaryRestriction" or "PhysicalLimitation" or "ProfileSync")
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
                    v.Category,
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

            if (!combined.Any()) return string.Empty;

            Console.WriteLine($"=== Member Health RAG: {combined.Count} relevant facts ===");

            return string.Join("\n", combined.Select(c => $"- {c}"));
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length || a.Length == 0) return 0;

            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            if (magA == 0 || magB == 0) return 0;
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        private static string CleanJson(string raw)
        {
            var clean = raw.Trim();
            if (clean.StartsWith("```json")) clean = clean.Substring(7);
            else if (clean.StartsWith("```")) clean = clean.Substring(3);
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
