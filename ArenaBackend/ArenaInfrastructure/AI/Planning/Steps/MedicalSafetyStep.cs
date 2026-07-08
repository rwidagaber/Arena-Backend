using ArenaApplication.AI.Planning;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.IServices;
using ArenaApplication.AI;
using ArenaInfrastructure.AI;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArenaInfrastructure.AI.Planning.Steps
{
    public class MedicalSafetyStep : IPlanningStep
    {
        private readonly IHealthIntelligenceService _healthIntelligence;
        private readonly IGeminiCompletionService _gemini;

        public MedicalSafetyStep(IHealthIntelligenceService healthIntelligence, IGeminiCompletionService gemini)
        {
            _healthIntelligence = healthIntelligence;
            _gemini = gemini;
        }

        public async Task ExecuteAsync(PlanningContext context)
        {
            // 1. Fetch strict guidelines from HealthIntelligenceService
            context.MedicalGuidelinesText = await _healthIntelligence.RetrieveMedicalGuidelinesAsync(context.HealthProfile);

            // 2. Parse safety constraints and substitutions using LLM if the user has any medical conditions
            var hp = context.HealthProfile;
            bool hasMedicalIssues = hp.Conditions.Count > 0 || 
                                    hp.Allergies.Count > 0 || 
                                    hp.Injuries.Count > 0 || 
                                    hp.Restrictions.Count > 0 || 
                                    hp.Medications.Count > 0 ||
                                    !string.IsNullOrWhiteSpace(hp.PhysicalLimitations) ||
                                    !string.IsNullOrWhiteSpace(hp.ChronicDiseases);

            if (hasMedicalIssues)
            {
                var prompt = $$"""
You are a medical safety validator for a fitness application.
Based on the following user health conditions:
- Conditions: {{string.Join(", ", hp.Conditions)}}
- Allergies: {{string.Join(", ", hp.Allergies)}}
- Injuries: {{string.Join(", ", hp.Injuries)}}
- Physical Limitations: {{hp.PhysicalLimitations ?? "None"}}
- Chronic Diseases: {{hp.ChronicDiseases ?? "None"}}
- Medications: {{string.Join(", ", hp.Medications)}}
- Dietary Restrictions: {{string.Join(", ", hp.Restrictions)}}

Generate:
1. Exercises that MUST be excluded.
2. Foods/Ingredients that MUST be excluded.
3. Safe, specific substitutions.

Return ONLY a valid JSON object in this format (no markdown):
{
  "excludedExercises": ["list of specific exercises to avoid"],
  "excludedFoods": ["list of specific foods or food groups to avoid"],
  "substitutions": ["Leg press instead of Squats", "Almond butter instead of peanut butter"]
}
""";

                try
                {
                    var response = await _gemini.GetCompletionAsync(prompt, new List<ChatMessageDto>(), "Analyze medical safety");
                    var cleanJson = AIHelper.CleanJson(response);

                    using var doc = JsonDocument.Parse(cleanJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("excludedExercises", out var exExc) && exExc.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in exExc.EnumerateArray())
                            context.SafetyInfo.ExcludedExercises.Add(item.GetString() ?? string.Empty);
                    }

                    if (root.TryGetProperty("excludedFoods", out var exFood) && exFood.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in exFood.EnumerateArray())
                            context.SafetyInfo.ExcludedFoods.Add(item.GetString() ?? string.Empty);
                    }

                    if (root.TryGetProperty("substitutions", out var subs) && subs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in subs.EnumerateArray())
                            context.SafetyInfo.Substitutions.Add(item.GetString() ?? string.Empty);
                    }
                }
                catch
                {
                    // Fallback to empty if LLM parsing fails
                }
            }
        }
    }
}
