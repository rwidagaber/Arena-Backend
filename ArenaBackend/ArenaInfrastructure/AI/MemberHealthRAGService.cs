using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ArenaApplication.Dtos.HealthIntelligence;
using ArenaDomain.Entities;

namespace ArenaInfrastructure.AI
{
    /// <summary>
    /// RAG service for member health data.
    /// - Embeddings: Gemini text-embedding-004 (768 dims)
    /// - Vector store: SQL Server via AppDbContext (DefaultConnection)
    /// - Vector similarity: Cosine distance calculated in-memory
    /// </summary>
    public class MemberHealthRAGService : IMemberHealthRAGService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IGeminiCompletionService _gemini;
        private readonly AppDbContext _context;

        private static readonly string[] CriticalCategories =
        [
            "Injury", "Disease", "HealthCondition", "Allergy",
            "Medication", "DietaryRestriction", "PhysicalLimitation", "ProfileSync"
        ];

        public MemberHealthRAGService(
            IEmbeddingService embeddingService,
            IGeminiCompletionService gemini,
            AppDbContext context)
        {
            _embeddingService = embeddingService;
            _gemini = gemini;
            _context = context;
        }

        // ── Ensure Schema ─────────────────────────────────────────────────────────

        public async Task EnsureSchemaAsync()
        {
            var createTableSql = """
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MemberHealthVectors' and xtype='U')
                BEGIN
                    CREATE TABLE MemberHealthVectors (
                        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        MemberProfileId UNIQUEIDENTIFIER NOT NULL,
                        Content NVARCHAR(1000) NOT NULL,
                        Category NVARCHAR(100) NOT NULL,
                        EmbeddingJson NVARCHAR(MAX) NULL,
                        RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        IsActive BIT NOT NULL DEFAULT 1,
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        UpdatedAt DATETIME2 NULL,
                        CreatedBy NVARCHAR(MAX) NULL,
                        UpdatedBy NVARCHAR(MAX) NULL,
                        DeletedAt DATETIME2 NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0,
                        CONSTRAINT FK_MemberHealthVectors_MemberProfiles FOREIGN KEY (MemberProfileId) REFERENCES MemberProfiles(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_MemberHealthVectors_MemberProfileId ON MemberHealthVectors(MemberProfileId);
                END
                """;
            await _context.Database.ExecuteSqlRawAsync(createTableSql);
        }

        // ── Save ──────────────────────────────────────────────────────────────────

        public async Task SaveHealthInfoAsync(Guid memberProfileId, string content, string category)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var exists = await _context.MemberHealthVectors
                .AnyAsync(v => v.MemberProfileId == memberProfileId && v.Content == content && v.IsActive);

            if (exists)
                return;

            var embedding = await _embeddingService.GetEmbeddingAsync(content);
            string? embeddingJson = null;
            if (embedding.Length > 0)
            {
                embeddingJson = JsonSerializer.Serialize(embedding);
            }
            else
            {
                Console.WriteLine($"[RAG] ⚠️ Saving without embedding (Gemini call failed): {content}");
            }

            var vector = new MemberHealthVector
            {
                Id = Guid.NewGuid(),
                MemberProfileId = memberProfileId,
                Content = content,
                Category = string.IsNullOrWhiteSpace(category) ? "ChatMention" : category,
                EmbeddingJson = embeddingJson,
                RecordedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.MemberHealthVectors.Add(vector);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[RAG] ✅ Saved health vector [{category}] for member {memberProfileId} in SQL Server");
        }

        // ── Sync profile data ────────────────────────────────────────────────────

        public async Task SyncProfileHealthDataAsync(Guid memberProfileId)
        {
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

            if (profile == null)
                return;

            // Soft-delete stale ProfileSync rows before re-inserting fresh ones
            var staleVectors = await _context.MemberHealthVectors
                .Where(v => v.MemberProfileId == profile.Id && v.Category == "ProfileSync" && v.IsActive)
                .ToListAsync();

            foreach (var v in staleVectors)
            {
                v.IsActive = false;
            }
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(profile.Injuries))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following injury: {profile.Injuries}", "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.HealthConditions))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following health condition: {profile.HealthConditions}", "ProfileSync");

            if (!string.IsNullOrWhiteSpace(profile.DietaryRestrictions))
                await SaveHealthInfoAsync(profile.Id, $"Member has the following dietary restriction: {profile.DietaryRestrictions}", "ProfileSync");
        }

        // ── Extract health facts from chat ───────────────────────────────────────

        private static string RemoveKeyword(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return text;

            var parts = text.Split(new[] { ',', ';', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !p.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            return string.Join(", ", parts);
        }

        public async Task SoftDeleteByKeywordAsync(Guid memberProfileId, string category, string keyword)
        {
            var lowercaseKeyword = keyword.ToLowerInvariant();
            var vectors = await _context.MemberHealthVectors
                .Where(v => v.MemberProfileId == memberProfileId && v.Category == category && v.IsActive)
                .ToListAsync();

            foreach (var v in vectors)
            {
                if (v.Content.ToLowerInvariant().Contains(lowercaseKeyword))
                {
                    v.IsActive = false;
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"[RAG] ✅ Soft-deleted health vectors containing keyword '{keyword}' in category '{category}' for member {memberProfileId}");
        }

        public async Task ExtractAndSaveFromChatAsync(Guid memberProfileId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return;

            var extractPrompt = $$"""
You are a health information extractor for a gym app.
Analyze this user message and determine:
1. If the user mentions any NEW injury, pain, disease, diagnosed health condition, illness, allergy, medication, dietary restriction, pregnancy, surgery, or physical limitation that should be remembered for future AI plans.
2. If the user reports RECOVERY or removal of any previously mentioned injury, condition, limitation, allergy, or dietary restriction (e.g. "I recovered from my ACL injury", "I'm no longer vegan", "my back pain is gone").

Message: "{{userMessage}}"

Return ONLY valid JSON, no markdown formatting:
{
  "hasHealthInfo": true or false,
  "items": [
    {
      "extractedInfo": "clear English sentence describing one NEW health fact (e.g. 'Member has knee pain')",
      "category": "Injury" or "Disease" or "HealthCondition" or "Allergy" or "Medication" or "DietaryRestriction" or "PhysicalLimitation"
    }
  ],
  "hasRecoveryInfo": true or false,
  "recoveredItems": [
    {
      "keyword": "a single English keyword identifying the recovered item (e.g. 'ACL', 'Knee Pain', 'Diabetes', 'Vegan', 'Peanuts')",
      "category": "Injury" or "Disease" or "HealthCondition" or "Allergy" or "Medication" or "DietaryRestriction" or "PhysicalLimitation"
    }
  ]
}

Only flag actual member health facts or recoveries, not general questions.
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

                if (result == null) return;

                var profile = await _context.MemberProfiles
                    .FirstOrDefaultAsync(p => p.Id == memberProfileId || p.UserId == memberProfileId);

                if (profile == null) return;

                bool profileUpdated = false;

                // Handle recoveries
                if (result.HasRecoveryInfo && result.RecoveredItems != null)
                {
                    foreach (var rec in result.RecoveredItems.Where(r => !string.IsNullOrWhiteSpace(r.Keyword)))
                    {
                        // 1. Soft delete from database
                        await SoftDeleteByKeywordAsync(profile.Id, rec.Category, rec.Keyword);

                        // 2. Update SQL Server profile legacy fields
                        if (rec.Category == "Injury" && !string.IsNullOrEmpty(profile.Injuries))
                        {
                            profile.Injuries = RemoveKeyword(profile.Injuries, rec.Keyword);
                            profileUpdated = true;
                        }
                        else if (rec.Category == "DietaryRestriction" && !string.IsNullOrEmpty(profile.DietaryRestrictions))
                        {
                            profile.DietaryRestrictions = RemoveKeyword(profile.DietaryRestrictions, rec.Keyword);
                            profileUpdated = true;
                        }
                        else if ((rec.Category == "Disease" || rec.Category == "HealthCondition" || rec.Category == "Allergy") && !string.IsNullOrEmpty(profile.HealthConditions))
                        {
                            profile.HealthConditions = RemoveKeyword(profile.HealthConditions, rec.Keyword);
                            profileUpdated = true;
                        }

                        // Also clean up structured HealthProfileJson if exists
                        if (!string.IsNullOrWhiteSpace(profile.HealthProfileJson))
                        {
                            try
                            {
                                var healthProfile = JsonSerializer.Deserialize<HealthProfileDto>(profile.HealthProfileJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (healthProfile != null)
                                {
                                    healthProfile.Conditions = healthProfile.Conditions.Where(c => !c.Contains(rec.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                                    healthProfile.Allergies = healthProfile.Allergies.Where(a => !a.Contains(rec.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                                    healthProfile.Injuries = healthProfile.Injuries.Where(i => !i.Contains(rec.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                                    healthProfile.Restrictions = healthProfile.Restrictions.Where(r => !r.Contains(rec.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                                    healthProfile.Medications = healthProfile.Medications.Where(m => !m.Contains(rec.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                                    
                                    profile.HealthProfileJson = JsonSerializer.Serialize(healthProfile);
                                    profileUpdated = true;
                                }
                            }
                            catch { }
                        }
                    }
                }

                // Handle new health items
                if (result.HasHealthInfo && result.Items != null)
                {
                    var items = result.Items.Count > 0
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
                        if (IsValidHealthInfo(userMessage, item.ExtractedInfo))
                        {
                            await SaveHealthInfoAsync(profile.Id, item.ExtractedInfo, item.Category);
                        }
                        else
                        {
                            Console.WriteLine($"[RAG] ⚠️ Rejected invalid health extraction: '{item.ExtractedInfo}'");
                        }
                    }
                }

                if (profileUpdated)
                {
                    _context.MemberProfiles.Update(profile);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RAG] Health extraction/recovery failed: {ex.Message}");
            }
        }

        // ── Retrieve context via in-memory Cosine search ──────────────────────────────

        public async Task<string> GetRelevantHealthContextAsync(Guid memberProfileId, string query, int topK = 5)
        {
            // 1. Fetch active vectors for the member
            var allVectors = await _context.MemberHealthVectors
                .Where(v => v.MemberProfileId == memberProfileId && v.IsActive)
                .ToListAsync();

            // Always separate critical health categories (injuries, diseases, etc.)
            var criticalContent = allVectors
                .Where(v => CriticalCategories.Contains(v.Category))
                .Select(v => v.Content)
                .ToList();

            // Get query embedding for semantic search
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);

            if (queryEmbedding.Length == 0 || allVectors.Count == 0)
            {
                // Fallback: return critical items only
                return criticalContent.Count > 0
                    ? string.Join("\n", criticalContent.Select(c => $"- {c}"))
                    : string.Empty;
            }

            // Calculate cosine distance in memory
            var semanticHits = new List<(string Content, double Distance)>();
            foreach (var v in allVectors)
            {
                if (string.IsNullOrEmpty(v.EmbeddingJson)) continue;

                try
                {
                    var embedding = JsonSerializer.Deserialize<float[]>(v.EmbeddingJson);
                    if (embedding != null && embedding.Length == queryEmbedding.Length)
                    {
                        var dist = CosineDistance(queryEmbedding, embedding);
                        semanticHits.Add((v.Content, dist));
                    }
                }
                catch { }
            }

            // cosine distance < 0.55 ≈ cosine similarity > 0.45
            var relevant = semanticHits
                .Where(r => r.Distance < 0.55)
                .OrderBy(r => r.Distance)
                .Take(topK)
                .Select(r => r.Content);

            var combined = criticalContent.Union(relevant).Distinct().ToList();

            Console.WriteLine($"[RAG] 🔍 SQL Server similarity: {semanticHits.Count} candidates → {combined.Count} returned for query '{query}'");

            return combined.Count > 0
                ? string.Join("\n", combined.Select(c => $"- {c}"))
                : string.Empty;
        }

        public async Task<bool> HasHealthInfoAsync(Guid memberProfileId)
        {
            return await _context.MemberHealthVectors
                .AnyAsync(v => v.MemberProfileId == memberProfileId && v.IsActive && CriticalCategories.Contains(v.Category));
        }

        private static double CosineDistance(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) return 1.0;
            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }
            if (normA == 0.0 || normB == 0.0) return 1.0;
            return 1.0 - (dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

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

        public static bool IsValidHealthInfo(string userMessage, string extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return false;

            var cleanUser = userMessage.Trim().ToLowerInvariant().Replace("?", "").Replace(".", "").Replace("!", "");
            var cleanExtracted = extractedInfo.Trim().ToLowerInvariant().Replace("?", "").Replace(".", "").Replace("!", "");

            // 1. If it's identical or highly similar to the entire user message
            if (cleanExtracted == cleanUser || (cleanUser.Contains(cleanExtracted) && cleanUser.Length - cleanExtracted.Length < 10))
                return false;

            // 2. Reject if the extracted item contains words that look like questions or commands
            string[] questionWords = [
                "what", "do i", "tell me", "show me", "can you", "could you", "do you", "remember", "list", "have i", "how", "injury", "injuries", "disease", "diseases", "allergy", "allergies", "condition", "conditions",
                "قول", "وريني", "عندي", "ايه", "إيه", "هل", "فاكر", "تعرف", "مسجل", "الأمراض", "الامراض", "الإصابات", "الاصابات", "حساسية"
            ];
            
            if (questionWords.Any(w => cleanExtracted.StartsWith(w) || cleanExtracted.Contains(" " + w)))
            {
                string[] retrievalWords = ["tell", "show", "what", "diseases do i", "injuries do i", "allergies do i", "هل عندي", "قولي", "وريني", "عارف", "فاكر"];
                if (retrievalWords.Any(w => cleanExtracted.Contains(w)))
                    return false;
            }

            return true;
        }
    }

    public class HealthExtractResult
    {
        public bool HasHealthInfo { get; set; }
        public List<HealthExtractItem> Items { get; set; } = [];
        public bool HasRecoveryInfo { get; set; }
        public List<HealthRecoveryItem> RecoveredItems { get; set; } = [];
        public string ExtractedInfo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class HealthExtractItem
    {
        public string ExtractedInfo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class HealthRecoveryItem
    {
        public string Keyword { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
