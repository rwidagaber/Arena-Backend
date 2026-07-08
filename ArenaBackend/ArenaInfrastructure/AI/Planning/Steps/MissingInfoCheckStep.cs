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
    public class MissingInfoCheckStep : IPlanningStep
    {
        private readonly IGeminiCompletionService _gemini;

        public MissingInfoCheckStep(IGeminiCompletionService gemini)
        {
            _gemini = gemini;
        }

        public async Task ExecuteAsync(PlanningContext context)
        {
            var hp = context.HealthProfile;
            var profile = context.Profile;

            var prompt = $$"""
You are a professional fitness coach checking if enough information exists to generate a safe and highly customized fitness plan.
Plan type requested: {{context.PlanType}}

Here is what we know about the user:
- Goal: {{context.GoalInfo.PrimaryGoal}}
- Age/Gender: {{DateTime.UtcNow.Year - profile.DateOfBirth.Year}} / {{profile.Gender}}
- Weight/Height: {{profile.Weight}}kg / {{profile.Height}}cm
- Target Weight: {{profile.TargetWeight}}kg
- Experience: {{profile.FitnessExperience}}
- Available Equipment: {{profile.Equipment}}
- Activity Level: {{profile.ActivityLevel}}
- Medical/Injuries: Conditions={{string.Join(", ", hp.Conditions)}}, Injuries={{string.Join(", ", hp.Injuries)}}, Restrictions={{string.Join(", ", hp.Restrictions)}}, Allergies={{string.Join(", ", hp.Allergies)}}, Medications={{string.Join(", ", hp.Medications)}}, Physical Limitations={{hp.PhysicalLimitations}}, Chronic Diseases={{hp.ChronicDiseases}}
- Sleep/Schedule: Sleep={{hp.SleepHours}} hrs, Schedule={{hp.DailySchedule}}, Preferred Time={{hp.PreferredWorkoutTime}}, Lifestyle={{hp.Lifestyle}}
- Food Preferences: {{hp.FoodPreferences}}

Current message from user: "{{context.UserMessage}}"

CRITICAL POLICY (FIRST RESPONSE POLICY):
- Missing optional preferences (such as workout frequency, preferred split, workout time, favorite exercises, preferred days, training location, training style, or equipment) must NEVER block plan generation.
- If these are missing, proceed with isMissingInfo = false and let downstream steps assume intelligent defaults.
- Do NOT block plan generation for standard or historical injuries/conditions (like knee pain, ACL injury, shoulder pain, back pain, diabetes, hypertension, allergies). Instead, you MUST proceed with isMissingInfo = false and generate a safe plan immediately that automatically avoids putting stress on the affected area (e.g., upper body focus or knee-friendly movements for ACL/knee pain) and state these safety assumptions.
- You may ONLY set "isMissingInfo": true and ask follow-up questions when it is impossible to create a medically safe plan (specifically: active chest pain during exercise, active heart palpitations, pregnancy without trimester information, or very recent major surgery [less than 4 weeks ago] with unknown medical restrictions).
- NEVER ask questions about fitness experience level, goal, equipment, or frequency, as these are already either in the database profile or can be safely defaulted.

Return ONLY a valid JSON object in this format (no markdown):
{
  "isMissingInfo": true/false,
  "followUpQuestions": ["question 1", "question 2"],
  "clarificationMessage": "A polite, friendly coaching message in the user's language (English or Arabic depending on context.IsArabic) explaining that you need a couple of details before crafting the perfect plan, followed by the bulleted questions. If context.IsArabic is true, return Arabic."
}
""";

            try
            {
                var response = await _gemini.GetCompletionAsync(prompt, new List<ChatMessageDto>(), "Check missing information");
                var cleanJson = AIHelper.CleanJson(response);

                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("isMissingInfo", out var isMiss) && (isMiss.ValueKind == JsonValueKind.True || isMiss.ValueKind == JsonValueKind.False))
                {
                    context.IsMissingInfo = isMiss.GetBoolean();
                }

                if (root.TryGetProperty("followUpQuestions", out var questions) && questions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var q in questions.EnumerateArray())
                    {
                        context.FollowUpQuestions.Add(q.GetString() ?? string.Empty);
                    }
                }

                if (root.TryGetProperty("clarificationMessage", out var msg) && msg.ValueKind == JsonValueKind.String)
                {
                    context.ClarificationMessage = msg.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // Fallback to not missing if LLM fails, so we don't block the user
                context.IsMissingInfo = false;
            }

            // Deterministic First Response Policy Override
            if (!HasMedicalSafetyBlocker(context))
            {
                context.IsMissingInfo = false;
                context.FollowUpQuestions.Clear();
                context.ClarificationMessage = string.Empty;
            }
        }

        private bool HasMedicalSafetyBlocker(PlanningContext context)
        {
            var msg = context.UserMessage?.ToLowerInvariant() ?? string.Empty;
            
            // Check for chest pain
            if (msg.Contains("chest pain") || msg.Contains("pain in my chest") || msg.Contains("وجع في الصدر") || msg.Contains("ألم في الصدر") || msg.Contains("الم في الصدر"))
                return true;

            // Check for heart palpitations
            if (msg.Contains("palpitation") || msg.Contains("heart beat") || msg.Contains("ضربات قلب") || msg.Contains("تسارع ضربات"))
                return true;

            // Check for pregnancy without trimester
            if ((msg.Contains("pregnant") || msg.Contains("pregnancy") || msg.Contains("حامل") || msg.Contains("حمل")) && 
                !(msg.Contains("trimester") || msg.Contains("month") || msg.Contains("شهر") || msg.Contains("أسابيع") || msg.Contains("اسابيع") || msg.Contains("week")))
                return true;

            // Check for very recent surgery
            if (msg.Contains("surgery") || msg.Contains("operation") || msg.Contains("جراحة") || msg.Contains("عملية"))
                return true;

            // Check for unexplained joint pain or serious joint pain
            if (msg.Contains("joint pain") || msg.Contains("severe joint") || msg.Contains("unexplained pain") || msg.Contains("ألم في المفاصل") || msg.Contains("الم مفاصل"))
                return true;

            // Check for conflicting medical information (e.g. user says they are fine but also diabetic/hypertensive/injured severely)
            // But we already handle standard injuries (knee, ACL, diabetes) as non-blocking. 
            // So we only block if user specifically reports active acute symptoms.

            return false;
        }
    }
}
