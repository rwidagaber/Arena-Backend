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
    public class GoalAndTimeAssessmentStep : IPlanningStep
    {
        private readonly IGeminiCompletionService _gemini;

        public GoalAndTimeAssessmentStep(IGeminiCompletionService gemini)
        {
            _gemini = gemini;
        }

        public async Task ExecuteAsync(PlanningContext context)
        {
            // 1. Detect Goal from user message vs database
            string? messageGoal = null;
            string? durationStr = null;

            if (!string.IsNullOrWhiteSpace(context.MessagePreferences))
            {
                try
                {
                    using var doc = JsonDocument.Parse(context.MessagePreferences);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("goal", out var g) && g.ValueKind == JsonValueKind.String)
                        messageGoal = g.GetString();
                    if (root.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.String)
                        durationStr = d.GetString();
                }
                catch { }
            }

            // Prioritize message goal over DB goal only if the message goal is specific,
            // or if the DB goal is missing/generic.
            bool isDbGoalSpecific = IsSpecificGoal(context.Profile.Goal);
            bool isMsgGoalSpecific = IsSpecificGoal(messageGoal);

            if (isMsgGoalSpecific || (!isDbGoalSpecific && !string.IsNullOrWhiteSpace(messageGoal)))
            {
                context.GoalInfo.PrimaryGoal = messageGoal!;
                context.GoalInfo.GoalSource = "Message";
                
                // Update profile goal temporarily so plan generator uses it
                context.Profile.Goal = messageGoal;
            }
            else if (!string.IsNullOrWhiteSpace(context.Profile.Goal))
            {
                context.GoalInfo.PrimaryGoal = context.Profile.Goal;
                context.GoalInfo.GoalSource = "Database";
            }
            else
            {
                // Goal is missing, default to General Fitness immediately
                context.GoalInfo.PrimaryGoal = "General Fitness";
                context.GoalInfo.GoalSource = "Fallback";
                context.Profile.Goal = "General Fitness";
            }

            if (!string.IsNullOrWhiteSpace(durationStr))
            {
                context.GoalInfo.DurationRequested = durationStr;

                // 2. Assess time feasibility using LLM
                var prompt = $$"""
You are a professional fitness coach. Determine if the user's fitness goal is realistically and safely achievable within the requested duration.
Never promise unrealistic transformations. If it's impossible or unsafe, politely explain why and provide a safe, realistic alternative.

Goal: {{context.GoalInfo.PrimaryGoal}}
Requested Duration: {{durationStr}}
User Profile: Age={{DateTime.UtcNow.Year - context.Profile.DateOfBirth.Year}}, Weight={{context.Profile.Weight ?? 0}}kg, Height={{context.Profile.Height ?? 0}}cm, Experience={{context.Profile.FitnessExperience ?? "Beginner"}}

Return ONLY a valid JSON object in this format (no markdown):
{
  "isRealistic": true/false,
  "isPartiallyRealistic": true/false,
  "feasibilityExplanation": "A friendly, empathetic explanation in the user's language (English or Arabic depending on context.IsArabic). If context.IsArabic is true, return Arabic."
}
""";

                try
                {
                    // Pass IsArabic parameter for localizing
                    var systemPrompt = context.IsArabic ? "أنت مدرب لياقة بدنية محترف تجيب باللغة العربية" : "You are a professional fitness coach answering in English";
                    var response = await _gemini.GetCompletionAsync(systemPrompt + "\n" + prompt, new List<ChatMessageDto>(), "Analyze feasibility");
                    var cleanJson = AIHelper.CleanJson(response);

                    using var resDoc = JsonDocument.Parse(cleanJson);
                    var root = resDoc.RootElement;
                    if (root.TryGetProperty("isRealistic", out var isReal) && isReal.ValueKind == JsonValueKind.True || isReal.ValueKind == JsonValueKind.False)
                        context.GoalInfo.IsDurationRealistic = isReal.GetBoolean();
                    if (root.TryGetProperty("isPartiallyRealistic", out var isPart) && isPart.ValueKind == JsonValueKind.True || isPart.ValueKind == JsonValueKind.False)
                        context.GoalInfo.IsDurationPartiallyRealistic = isPart.GetBoolean();
                    if (root.TryGetProperty("feasibilityExplanation", out var exp) && exp.ValueKind == JsonValueKind.String)
                        context.GoalInfo.FeasibilityExplanation = exp.GetString() ?? string.Empty;
                }
                catch
                {
                    context.GoalInfo.IsDurationRealistic = true;
                    context.GoalInfo.FeasibilityExplanation = context.IsArabic 
                        ? $"سنعمل على تحقيق هدفك خلال {durationStr} بشكل آمن."
                        : $"We will work on achieving your goal in {durationStr} safely.";
                }
            }
        }

        private static bool IsSpecificGoal(string? goal)
        {
            if (string.IsNullOrWhiteSpace(goal)) return false;
            var clean = goal.ToLowerInvariant();
            return !clean.Contains("general") && !clean.Contains("improve") && !clean.Contains("maintain") && !clean.Contains("balance");
        }
    }
}
