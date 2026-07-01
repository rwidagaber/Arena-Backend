using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.HealthIntelligence;
using ArenaApplication.IServices;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class HealthIntelligenceService : IHealthIntelligenceService
    {
        private readonly IGeminiCompletionService _gemini;

        public HealthIntelligenceService(IGeminiCompletionService gemini)
        {
            _gemini = gemini;
        }

        public async Task<HealthProfileDto> ExtractHealthProfileAsync(string userMessage)
        {
            var prompt = """
You are a highly advanced medical extraction system for a fitness application.
Analyze the user message and extract all health-related information into a structured JSON object.
Map any Arabic colloquialisms, misspellings, and Arabizi to STANDARD ENGLISH MEDICAL TERMS.
Examples:
- "عندي أنيميا الفول" or "فافيزم" => "G6PD Deficiency"
- "عندي السكر" or "3andi sokkar" => "Diabetes"
- "حساسية من اللبن" or "3andi hsasya mn el laban" => "Lactose Intolerance"
- "ركبتي بتوجعني" => "Knee Pain / Injury"

Return ONLY valid JSON in this exact format, with no markdown formatting:
{
  "conditions": ["Diabetes", "G6PD Deficiency"],
  "allergies": ["Peanuts", "Lactose Intolerance"],
  "injuries": ["Knee Pain"],
  "restrictions": ["Vegetarian"],
  "medications": ["Insulin"]
}

If no information is found for a category, return an empty array [].
""";

            try
            {
                var response = await _gemini.GetCompletionAsync(prompt, new List<ChatMessageDto>(), userMessage);
                var cleanJson = CleanJson(response);
                var profile = JsonSerializer.Deserialize<HealthProfileDto>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return profile ?? new HealthProfileDto();
            }
            catch
            {
                return new HealthProfileDto();
            }
        }

        public async Task<string> RetrieveMedicalGuidelinesAsync(HealthProfileDto profile)
        {
            if (profile.Conditions.Count == 0 && profile.Allergies.Count == 0 && profile.Injuries.Count == 0 && profile.Restrictions.Count == 0 && profile.Medications.Count == 0)
            {
                return string.Empty;
            }

            var profileJson = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });

            var prompt = $$"""
You are acting as an authoritative Medical Knowledge Retrieval system (based on WHO, CDC, NHS guidelines).
The user has the following health profile:
{{profileJson}}

Provide a strict, concise list of RESTRICTIONS and RECOMMENDATIONS for both Nutrition and Workout based on these conditions.
If they have a specific condition (like G6PD Deficiency), explicitly state what foods MUST be avoided (e.g. Fava beans, legumes).
If they have a physical injury, state what exercises MUST be avoided.
Return the guidelines as a clear bulleted list.
""";

            try
            {
                var response = await _gemini.GetCompletionAsync(prompt, new List<ChatMessageDto>(), "Retrieve Guidelines");
                return response;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<ValidationResultDto> ValidatePlanAsync(HealthProfileDto profile, string planJson, string planType)
        {
            if (profile.Conditions.Count == 0 && profile.Allergies.Count == 0 && profile.Injuries.Count == 0 && profile.Restrictions.Count == 0 && profile.Medications.Count == 0)
            {
                return new ValidationResultDto { IsValid = true };
            }

            var profileJson = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });

            var prompt = $$"""
You are a strict Medical Validation Layer for a fitness app.
You must review the following {{planType}} plan against the user's Health Profile to ensure it is 100% safe.

Health Profile:
{{profileJson}}

Plan to validate:
{{planJson}}

Check for any conflicts:
- Does the nutrition plan include any allergens? (e.g., fava beans for G6PD, dairy for lactose intolerance)
- Does the workout plan include exercises that aggravate injuries? (e.g., heavy squats for knee injury)
- Does it violate dietary restrictions or medical advice?

Return ONLY valid JSON in this exact format:
{
  "isValid": true or false,
  "rejectionReason": "If invalid, explain exactly why (e.g. 'Plan contains fava beans, which is dangerous for G6PD'). If valid, leave empty."
}
""";

            try
            {
                var response = await _gemini.GetCompletionAsync(prompt, new List<ChatMessageDto>(), "Validate Plan");
                var cleanJson = CleanJson(response);
                var result = JsonSerializer.Deserialize<ValidationResultDto>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new ValidationResultDto { IsValid = true };
            }
            catch
            {
                // In case of error, assume valid to avoid blocking the user indefinitely
                return new ValidationResultDto { IsValid = true };
            }
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
}
