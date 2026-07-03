using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArenaApplication.Dtos.WorkoutPlan;

namespace ArenaInfrastructure.AI
{
    public static class WorkoutLocalization
    {
        private static readonly Dictionary<string, string> ExerciseTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Dumbbell Bench Press", "ضغط صدر بالدامبل" },
            { "Dumbbell Shoulder Press", "ضغط كتف بالدامبل" },
            { "Lat Pulldown", "سحب أمامي" },
            { "Cable Row", "تجديف بالكابل" },
            { "Pec Deck Fly", "تفتيح صدر" },
            { "Hyperextension", "تمديد أسفل الظهر" },
            { "Stationary Bike Cycling", "الدراجة الثابتة" },
            { "Tricep Pushdown", "دفع الترايسبس بالكابل" },
            { "Lying Leg Raise", "رفع الساقين" },
            { "Lying Leg Curl", "ثني الساق الخلفية" },
            { "Yoga Mat Plank", "بلانك" },
            { "Dumbbell Hammer Curl", "تمرين المطرقة بالدامبل" },
            
            // Other catalog/prompt/fallback exercises
            { "Plank", "بلانك" },
            { "Walking or Bike Warmup", "إحماء مشي أو دراجة" },
            { "Scapular Retraction", "تراجع لوح الكتف" },
            { "Cable Row Light", "تجديف بالكابل خفيف" },
            { "Wall Push-up", "ضغط على الحائط" },
            { "Dead Bug", "تمرين الحشرة الميتة" },
            { "Chest Press Machine", "جهاز ضغط الصدر" },
            { "Seated Leg Curl", "ثني الساق الخلفية جالساً" },
            { "Glute Bridge", "جسر الأرداف" },
            { "Leg Extension Machine", "جهاز تمديد الساقين" },
            { "Leg Press", "دفع الساقين بالآلة" },
            { "Calf Raise", "رفع السمانة" },
            { "Incline Walk", "مشي على منحدر" },
            { "Machine Chest Press", "ضغط الصدر بالآلة" },
            { "Assisted Pull-up", "عقلة بمساعدة" },
            { "Cable Face Pull", "سحب كابل للوجه" },
            { "Suitcase Hold", "تمرين الحقيبة" },
            { "Farmer Carry", "مشي المزارع" },
            { "Mobility Flow", "تمارين الحركية" },
            { "Seated Shoulder Press", "ضغط كتف جالساً" },
            { "Biceps Curl", "تبادل بايسبس" },
            { "Hip Thrust", "دفع الورك" },
            { "Bike Moderate Pace", "دراجة بسرعة معتدلة" }
        };

        private static readonly Dictionary<string, string> DayTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Monday", "الإثنين" },
            { "Tuesday", "الثلاثاء" },
            { "Wednesday", "الأربعاء" },
            { "Thursday", "الخميس" },
            { "Friday", "الجمعة" },
            { "Saturday", "السبت" },
            { "Sunday", "الأحد" }
        };

        public static bool IsArabic(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Any(c => c >= 0x0600 && c <= 0x06FF);
        }

        public static string TranslateExercise(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            
            var trimmedName = name.Trim();
            if (ExerciseTranslations.TryGetValue(trimmedName, out var translated))
            {
                return translated;
            }

            return name;
        }

        public static string TranslateDay(string dayName)
        {
            if (string.IsNullOrWhiteSpace(dayName)) return dayName;
            var trimmed = dayName.Trim();
            
            if (DayTranslations.TryGetValue(trimmed, out var translated))
            {
                return translated;
            }

            foreach (var kvp in DayTranslations)
            {
                if (trimmed.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return dayName
                .Replace("Day", "اليوم", StringComparison.OrdinalIgnoreCase)
                .Replace("Rest", "راحة", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetLocalizedPlanName(string goal, bool isArabic)
        {
            var cleanGoal = goal?.ToLowerInvariant() ?? "";

            if (isArabic)
            {
                if (cleanGoal.Contains("loss") || cleanGoal.Contains("lose") || cleanGoal.Contains("fat") || cleanGoal.Contains("خسارة"))
                    return "خطة خسارة الوزن";
                if (cleanGoal.Contains("gain") || cleanGoal.Contains("bulk") || cleanGoal.Contains("muscle") || cleanGoal.Contains("عضلات") || cleanGoal.Contains("بناء"))
                    return "خطة بناء العضلات";
                if (cleanGoal.Contains("strength") || cleanGoal.Contains("power") || cleanGoal.Contains("قوة"))
                    return "خطة القوة";
                if (cleanGoal.Contains("beginner") || cleanGoal.Contains("start") || cleanGoal.Contains("مبتدئ"))
                    return "خطة اللياقة للمبتدئين";

                return "خطة اللياقة البدنية";
            }
            else
            {
                if (cleanGoal.Contains("loss") || cleanGoal.Contains("lose") || cleanGoal.Contains("fat") || cleanGoal.Contains("خسارة"))
                    return "Weight Loss Plan";
                if (cleanGoal.Contains("gain") || cleanGoal.Contains("bulk") || cleanGoal.Contains("muscle") || cleanGoal.Contains("عضلات") || cleanGoal.Contains("بناء"))
                    return "Muscle Gain Plan";
                if (cleanGoal.Contains("strength") || cleanGoal.Contains("power") || cleanGoal.Contains("قوة"))
                    return "Strength Training Plan";
                if (cleanGoal.Contains("beginner") || cleanGoal.Contains("start") || cleanGoal.Contains("مبتدئ"))
                    return "Beginner Fitness Plan";

                return "General Fitness Plan";
            }
        }

        public static string FormatArabicWorkoutPlan(WorkoutPlanDto plan, string successHeader, bool includeNutritionTip)
        {
            var sb = new StringBuilder();
            sb.AppendLine(successHeader);
            sb.AppendLine();
            sb.AppendLine("📋 اسم الخطة:");
            sb.AppendLine(plan.Name);
            sb.AppendLine();
            sb.AppendLine("📅 مدة الخطة:");
            sb.AppendLine($"{plan.DurationWeeks} أسابيع");
            sb.AppendLine();

            foreach (var day in plan.Days)
            {
                var dayName = TranslateDay(day.DayName);
                sb.AppendLine($"🏋️ {dayName}");
                sb.AppendLine();
                foreach (var ex in day.Exercises)
                {
                    var exName = TranslateExercise(ex.Name);
                    if (exName.ToLower().Contains("rest") || exName.Contains("راحة"))
                    {
                        sb.AppendLine($"• {exName} 😴");
                    }
                    else if (ex.Sets <= 1 && ex.Reps >= 20)
                    {
                        sb.AppendLine($"• {exName} — {ex.Reps} دقيقة");
                    }
                    else
                    {
                        sb.AppendLine($"• {exName} — {ex.Sets} مجموعات × {ex.Reps} تكرار");
                    }
                }
                sb.AppendLine();
            }

            if (includeNutritionTip)
            {
                sb.AppendLine("💡 إذا كنت ترغب أيضاً في خطة غذائية مناسبة لهدفك، فأخبرني بذلك.");
            }

            return sb.ToString().Trim();
        }
    }
}
