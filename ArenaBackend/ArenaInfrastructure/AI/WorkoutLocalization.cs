using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.Dtos.Nutrition;

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
            { "Bike Moderate Pace", "دراجة بسرعة معتدلة" },

            // Newly reported exercises
            { "Barbell Squat", "سكوات بالبار" },
            { "Dumbbell Bicep Curl", "تمرين بايسبس بالدامبل" },
            { "Standing Calf Raise", "رفع السمانة واقف" },
            { "Bicep Curl", "تمرين بايسبس" },
            { "Squat", "سكوات" }
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

        private static readonly Dictionary<string, string> MealTypeTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Breakfast", "الإفطار" },
            { "Morning Snack", "وجبة خفيفة صباحية" },
            { "Lunch", "الغداء" },
            { "Afternoon Snack", "وجبة خفيفة بعد الظهر" },
            { "Dinner", "العشاء" },
            { "Snack", "وجبة خفيفة" }
        };

        private static readonly Dictionary<string, string> MealNameTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Oatmeal with Banana and Peanut Butter", "شوفان مع الموز وزبدة الفول السوداني" },
            { "Greek Yogurt with Honey and Walnuts", "زبادي يوناني بالعسل والجوز" },
            { "Grilled Chicken and Brown Rice Bowl", "دجاج مشوي مع الأرز البني" },
            { "Tuna Salad with Whole Wheat Crackers", "سلطة تونة مع بسكويت القمح الكامل" },
            { "Baked Salmon with Sweet Potato", "سلمون مشوي مع البطاطا الحلوة" },
            { "Low-GI protein breakfast", "فطور بروتيني منخفض المؤشر الجلايسيمي" },
            { "Protein breakfast", "فطور بروتيني" },
            { "Legume power bowl", "سلطة البقوليات الغنية بالبروتين" },
            { "Lean protein bowl", "سلطة البروتين الصافي" },
            { "Steady blood sugar dinner", "عشاء متوازن لمستوى السكر" },
            { "Recovery dinner", "عشاء للاستشفاء العضلي" },
            { "Goal-support snack", "وجبة خفيفة داعمة لهدفك" }
        };

        private static readonly Dictionary<string, string> FoodDictionary = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Grilled Chicken", "دجاج مشوي" },
            { "Brown Rice Bowl", "طبق أرز بني" },
            { "Brown Rice", "أرز بني" },
            { "Tuna Salad", "سلطة تونة" },
            { "Whole Wheat Crackers", "بسكويت من القمح الكامل" },
            { "Whole Wheat", "القمح الكامل" },
            { "Baked Salmon", "سلمون مشوي" },
            { "Sweet Potato", "بطاطا حلوة" },
            { "Lean Protein Bowl", "طبق بروتين صافي" },
            { "Lean Protein", "بروتين صافي" },
            { "Greek Yogurt", "زبادي يوناني" },
            { "Protein Shake", "مخفوق البروتين" },
            { "Protein Powder", "بودرة البروتين" },
            { "Cottage Cheese", "جبن قريش" },
            { "Oatmeal", "شوفان" },
            { "Oats", "شوفان" },
            { "Chicken Breast", "صدر دجاج" },
            { "Chicken", "دجاج" },
            { "Rice", "أرز" },
            { "Fish", "سمك" },
            { "Egg Whites", "بياض بيض" },
            { "Egg White", "بياض بيض" },
            { "White Eggs", "بياض بيض" },
            { "White Egg", "بياض بيض" },
            { "Eggs", "بيض" },
            { "Egg", "بيض" },
            { "Banana", "موز" },
            { "Potato", "بطاطس" },
            { "Potatoes", "بطاطس" },
            { "Salmon", "سلمون" },
            { "Tuna", "تونة" },
            { "Yogurt", "زبادي" },
            { "Salad", "سلطة" },
            { "Steak", "ستيك" },
            { "Beef", "لحم بقري" },
            { "Turkey", "ديك رومي" },
            { "Peanut Butter", "زبدة الفول السوداني" },
            { "Honey", "عسل" },
            { "Walnuts", "جوز" },
            { "Walnut", "جوز" },
            { "Almonds", "لوز" },
            { "Almond", "لوز" },
            { "Nuts", "مكسرات" },
            { "Milk", "حليب" },
            { "Bread", "خبز" },
            { "Cheese", "جبن" },
            { "Hummus", "حمص" },
            { "Lentils", "عدس" },
            { "Chickpeas", "حمص" },
            { "Tofu", "توفو" },
            { "Apple", "تفاحة" },
            { "Fruit", "فواكه" },
            { "Vegetables", "خضروات" },
            { "Vegetable", "خضار" },
            { "Greens", "خضار ورقية" },
            { "Shake", "مخفوق" },
            { "Avocado", "أفوكادو" },
            { "Olive Oil", "زيت زيتون" },
            { "White", "أبيض" },
            { "Grilled", "مشوي" },
            { "Baked", "مشوي" },
            { "Boiled", "مسلوق" },
            { "With", "مع" },
            { "And", "و" },
            { "Or", "أو" },
            { "Bowl", "طبق" },
            { "Crackers", "بسكويت" },
            { "Seeds", "بذور" },
            { "Seed", "بذور" },
            { "Chia", "شيا" },
            { "Recovery", "استشفاء" },
            { "Steady", "مستقر" },
            { "Blood Sugar", "سكر الدم" },
            { "Flaxseeds", "بذور الكتان" },
            { "Flaxseed", "بذور الكتان" },
            { "Quinoa", "الكينوا" },
            { "Roasted", "مشوي" },
            { "Whey Protein", "بروتين واي" },
            { "Whey", "بروتين واي" },
            { "Broccoli", "بروكلي" },
            { "Green Beans", "فاصوليا خضراء" },
            { "Lemon Juice", "عصير ليمون" },
            { "Cinnamon", "قرفة" },
            { "Water", "ماء" },
            { "Chia seeds", "بذور الشيا" },
            { "Chia seed", "بذور الشيا" },
            { "Pumpkin seeds", "بذور اليقطين" },
            { "Sesame seeds", "بذور السمسم" },
            { "Sunflower seeds", "بذور عباد الشمس" },
            { "Lemon", "ليمون" },
            { "Lime", "ليمون أخضر" },
            { "Spinach", "سبانخ" }
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

        public static string TranslateMealType(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return type;
            var trimmed = type.Trim();
            if (MealTypeTranslations.TryGetValue(trimmed, out var translated))
            {
                return translated;
            }
            return type;
        }

        public static string FormatDecimal(decimal value)
        {
            if (value % 1 == 0)
            {
                return ((long)value).ToString();
            }
            return value.ToString("0.#");
        }

        public static string LocalizeUnits(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var pattern = @"(\d+(?:\.\d+)?)\s*(g|kg|ml|cups?|tbsps?|tsps?|slices?|pieces?|scoops?)\b";
            return Regex.Replace(text, pattern, m =>
            {
                var num = m.Groups[1].Value;
                var unit = m.Groups[2].Value.ToLowerInvariant();

                var translatedUnit = unit switch
                {
                    "g" => "جم",
                    "kg" => "كجم",
                    "ml" => "مل",
                    "cup" => "كوب",
                    "cups" => "أكواب",
                    "tbsp" => "ملعقة كبيرة",
                    "tbsps" => "ملعقة كبيرة",
                    "tsp" => "ملعقة صغيرة",
                    "tsps" => "ملعقة صغيرة",
                    "slice" => "شريحة",
                    "slices" => "شرائح",
                    "piece" => "قطعة",
                    "pieces" => "قطع",
                    "scoop" => "مكيال",
                    "scoops" => "مكيال",
                    _ => unit
                };

                // Remove decimal .00 inside formatted units
                if (decimal.TryParse(num, out var decVal))
                {
                    num = FormatDecimal(decVal);
                }

                return $"{num} {translatedUnit}";
            }, RegexOptions.IgnoreCase);
        }

        public static string TranslatePhrase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            
            var trimmed = text.Trim();
            
            if (MealNameTranslations.TryGetValue(trimmed, out var exact))
            {
                return exact;
            }

            var workingText = LocalizeUnits(trimmed);

            foreach (var kvp in FoodDictionary.OrderByDescending(k => k.Key.Length))
            {
                var pattern = @"\b" + Regex.Escape(kvp.Key) + @"\b";
                workingText = Regex.Replace(workingText, pattern, kvp.Value, RegexOptions.IgnoreCase);
            }

            workingText = workingText.Replace(",", "،");

            return workingText;
        }

        public static void LocalizeNutritionPlan(NutritionPlanResponseDto plan, bool isArabic)
        {
            if (isArabic && plan != null && plan.Meals != null)
            {
                foreach (var meal in plan.Meals)
                {
                    meal.MealType = TranslateMealType(meal.MealType);
                    meal.Name = TranslatePhrase(meal.Name);
                    meal.Ingredients = TranslatePhrase(meal.Ingredients);
                }
            }
        }

        public static string FormatNutritionPlan(NutritionPlanResponseDto plan, bool isArabic)
        {
            var sb = new StringBuilder();
            sb.AppendLine(isArabic
                ? "✅ تم إعداد خطتك الغذائية بنجاح! 🥗"
                : "✅ Your nutrition plan has been successfully prepared! 🥗");
            sb.AppendLine();
            sb.AppendLine(isArabic ? "📋 خطة التغذية" : "📋 Nutrition Plan");
            sb.AppendLine();
            
            sb.AppendLine(isArabic
                ? $"🔥 السعرات اليومية: {FormatDecimal(plan.DailyCalories)} سعر حراري | 💪 بروتين: {FormatDecimal(plan.ProteinGrams)} جم | 🍚 كارب: {FormatDecimal(plan.CarbsGrams)} جم | 🥑 دهون: {FormatDecimal(plan.FatGrams)} جم"
                : $"🔥 Daily Calories: {FormatDecimal(plan.DailyCalories)} kcal | 💪 Protein: {FormatDecimal(plan.ProteinGrams)}g | 🍚 Carbs: {FormatDecimal(plan.CarbsGrams)}g | 🥑 Fat: {FormatDecimal(plan.FatGrams)}g");
            sb.AppendLine();

            foreach (var meal in plan.Meals)
            {
                var mealType = isArabic ? TranslateMealType(meal.MealType) : meal.MealType;
                var mealName = isArabic ? TranslatePhrase(meal.Name) : meal.Name;
                var ingredients = isArabic ? TranslatePhrase(meal.Ingredients) : meal.Ingredients;

                sb.AppendLine($"🍽️ **{mealType}** — {mealName}");
                sb.AppendLine(isArabic
                    ? $"   {FormatDecimal(meal.Calories)} سعر حراري | بروتين: {FormatDecimal(meal.ProteinGrams)} جم | كارب: {FormatDecimal(meal.CarbsGrams)} جم"
                    : $"   {FormatDecimal(meal.Calories)} kcal | P: {FormatDecimal(meal.ProteinGrams)}g | C: {FormatDecimal(meal.CarbsGrams)}g | F: {FormatDecimal(meal.FatGrams)}g");
                sb.AppendLine($"   *{ingredients}*");
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        public static bool ContainsAny(string? text, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return values.Any(val => text.Contains(val, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetProactiveSubstitutionNotes(string healthContext, bool isArabic, bool isWorkout)
        {
            if (string.IsNullOrWhiteSpace(healthContext)) return string.Empty;

            var sb = new StringBuilder();

            if (isWorkout)
            {
                if (ContainsAny(healthContext, "knee", "acl", "meniscus", "ركب"))
                {
                    sb.AppendLine(isArabic
                        ? "💡 لاحظت من ملفك الصحي أنك تعاني من إصابة في الركبة/الرباط الصليبي، لذلك استبدلت تمارين السكوات والقفز بتمارين تقوية منخفضة التأثير وآمنة للركبة."
                        : "💡 I noticed from your saved profile that you have a knee/ACL injury, so I replaced squats and high-impact exercises with knee-friendly and low-impact strengthening exercises.");
                }
                if (ContainsAny(healthContext, "shoulder", "rotator", "كتف"))
                {
                    sb.AppendLine(isArabic
                        ? "💡 لاحظت من ملفك الصحي أنك تعاني من إصابة في الكتف، لذلك تجنبنا تمارين الضغط العلوي واستبدلناها بتمارين سحب آمنة ومريحة للمفاصل."
                        : "💡 I noticed from your saved profile that you have a shoulder injury, so I avoided overhead presses and substituted them with shoulder-safe movements.");
                }
                if (ContainsAny(healthContext, "back", "spine", "lumber", "ظهر"))
                {
                    sb.AppendLine(isArabic
                        ? "💡 لاحظت من ملفك الصحي أنك تعاني من آلاف/آلام الظهر، لذلك تجنبنا الضغط الثقيل على العمود الفقري ووفرنا تمارين بديلة تدعم استقرار الظهر."
                        : "💡 I noticed from your saved profile that you experience back pain, so I avoided heavy spinal loading and provided safer back-friendly alternatives.");
                }
            }
            else
            {
                if (ContainsAny(healthContext, "peanut", "فول سوداني"))
                {
                    sb.AppendLine(isArabic
                        ? "💡 لاحظت أن لديك حساسية من الفول السوداني، لذلك قمنا باستبعاد زبدة الفول السوداني تماماً واستخدمنا بدائل آمنة مثل زبدة اللوز."
                        : "💡 I noticed from your saved profile that you have a peanut allergy, so I completely excluded peanut butter and replaced it with safe alternatives like almond butter.");
                }
                if (ContainsAny(healthContext, "lactose", "dairy", "milk", "cheese", "yogurt", "حليب", "جبن", "لبن"))
                {
                    sb.AppendLine(isArabic
                        ? "💡 لاحظت أنك تعاني من حساسية اللاكتوز أو الألبان، لذلك قمنا باستبدال منتجات الألبان التقليدية ببدائل نباتية وخالية من اللاكتوز."
                        : "💡 I noticed you have lactose intolerance/dairy restriction, so I substituted dairy products with dairy-free alternatives.");
                }
            }

            return sb.ToString().Trim();
        }

        private static readonly Dictionary<string, string> HealthTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Anterior Cruciate Ligament Injury", "إصابة الرباط الصليبي الأمامي" },
            { "Anterior Cruciate Ligament (ACL) Injury", "إصابة الرباط الصليبي الأمامي (ACL)" },
            { "Knee Injury", "إصابة في الركبة" },
            { "Shoulder Injury", "إصابة في الكتف" },
            { "Lower Back Pain", "ألم أسفل الظهر" },
            { "Meniscus Tear", "تمزق الغضروف الهلالي" },
            { "Ankle Injury", "إصابة في الكاحل" },
            { "Diabetes", "السكري" },
            { "Hypertension", "ارتفاع ضغط الدم" },
            { "Asthma", "الربو" },
            { "Heart Disease", "أمراض القلب" },
            { "Arthritis", "التهاب المفاصل" },
            { "Peanut Allergy", "حساسية من الفول السوداني" },
            { "Peanuts", "حساسية من الفول السوداني" },
            { "Fish Allergy", "حساسية من السمك" },
            { "Seafood Allergy", "حساسية من المأكولات البحرية" },
            { "Milk Allergy", "حساسية من الحليب" },
            { "Lactose Intolerance", "عدم تحمل اللاكتوز" },
            { "Gluten Intolerance", "عدم تحمل الجلوتين" },
            { "Gluten Allergy", "حساسية من الجلوتين" },
            { "Egg Allergy", "حساسية من البيض" },
            { "Lemon Allergy", "حساسية من الليمون" },
            { "Berries Allergy", "حساسية من التوت" }
        };

        public static string LocalizeHealthString(string? rawInput, bool isArabic)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
                return isArabic ? "لا يوجد" : "None";

            if (!isArabic)
                return rawInput;

            var items = rawInput.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(i => i.Trim())
                               .Where(i => !string.IsNullOrEmpty(i))
                               .ToList();

            var translatedItems = items.Select(item =>
            {
                if (HealthTranslations.TryGetValue(item, out var translation))
                {
                    return translation;
                }
                return item; // Fallback to original value
            });

            return string.Join("، ", translatedItems);
        }
    }
}
