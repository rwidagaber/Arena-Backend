//using ArenaDomain.Entities;
//using ArenaDomain.Entities.User;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace ArenaApplication.AI
//{
//    public static class PromptBuilder
//    {
//        //public static string BuildWorkoutPrompt(MemberProfile profile)
//        //{
//        //    // Calculate age from DateOfBirth since no Age field
//        //    var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

//        //    return $$$"""
//        //You are a professional fitness trainer.
//        //Generate a detailed workout plan for:
//        //- Name: {profile.FirstName}
//        //- Age: {age}
//        //- Weight: {profile.Weight ?? 70}kg
//        //- Height: {profile.Height ?? 170}cm
//        //- Gender: {profile.Gender}
//        //- Goal: Muscle Gain

//        //Return ONLY valid JSON, no extra text:
//        //{{

//        //          "name": "...",
//        //  "durationWeeks": 4,
//        //  "days": [
//        //    {{

//        //              "dayName": "Monday",
//        //      "exercises": [
//        //        {{ "name": "...", "sets": 3, "reps": 10, "muscleGroup": "..." }}
//        //      ]
//        //    }}
//        //  ]
//        //}}
//        //""";
//        //}

//        // ✅ Fix — pass goal from user message
//        public static string BuildWorkoutPrompt(MemberProfile profile, string userMessage)
//        {
//            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

//            return $$$"""
//        You are a professional fitness trainer.
//        Generate a detailed workout plan for:
//        - Name: {profile.FirstName}
//        - Age: {age}
//        - Weight: {profile.Weight ?? 70}kg
//        - Height: {profile.Height ?? 170}cm
//        - Gender: {profile.Gender}
//        - User Request: {userMessage}

//        Understand the user's goal from their request and generate accordingly.

//        Return ONLY valid JSON, no extra text, no markdown:
//        {{

//                  "name": "...",
//          "durationWeeks": 4,
//          "days": [
//            {{

//                      "dayName": "Monday",
//              "exercises": [
//                {{ "name": "...", "sets": 3, "reps": 10, "muscleGroup": "..." }}
//              ]
//            }}
//          ]
//        }}
//        """;
//        }
//        public static string BuildNutritionPrompt(MemberProfile profile)
//        {
//            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

//            return $$$"""
//        You are a certified nutritionist.
//        Generate a nutrition plan for:
//        - Age: {age}
//        - Weight: {profile.Weight ?? 70}kg
//        - Height: {profile.Height ?? 170}cm
//        - Gender: {profile.Gender}
//        - Goal: Muscle Gain

//        Return ONLY valid JSON, no extra text:
//        {{

//                  "dailyCalories": 2200,
//          "proteinGrams": 150,
//          "carbsGrams": 220,
//          "fatGrams": 70,
//          "meals": [
//            {{

//                      "mealType": "Breakfast",
//              "name": "...",
//              "calories": 400,
//              "proteinGrams": 30,
//              "carbsGrams": 50,
//              "fatGrams": 10,
//              "ingredients": "..."
//            }}
//          ]
//        }}
//        """;
//        }

//        public static string BuildChatSystemPrompt(MemberProfile profile)
//        {
//            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

//            return $"""
//        You are Arena's AI fitness assistant.
//        Member info:
//        - Name: {profile.FirstName}
//        - Age: {age}
//        - Weight: {profile.Weight ?? 0}kg
//        - Height: {profile.Height ?? 0}cm
//        - Gender: {profile.Gender}

//        Help with fitness advice, workouts, nutrition, and gym bookings.
//        Be friendly and concise.
//        """;
//        }
//    }
//}


//using ArenaDomain.Entities;

//namespace ArenaApplication.AI
//{
//    public static class PromptBuilder
//    {
//        public static string BuildIntentDetectionPrompt()
//        {
//            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

//            return $$$"""
//                You are an intent detection system for a gym management app called Arena.
//                The user may write in English OR Arabic OR both mixed.

//                Analyze the user message carefully based on MEANING not exact words.
//                Return ONLY this JSON, no extra text, no markdown:
//                {{
//                  "intent": "workout" | "nutrition" | "booking" | "chat",
//                  "date": "YYYY-MM-DD or null",
//                  "time": "HH:mm or null"
//                }}

//                Intent rules:
//                - "workout"   → exercise, training, gym program, building muscle,
//                                losing weight through exercise,
//                                تمرين, برنامج رياضي, عايز اتمرن
//                - "nutrition" → food, eating, diet, meals, calories,
//                                what to eat, نظام غذائي, اكل, وجبات
//                - "booking"   → reserve, schedule, coming to gym, specific time/date,
//                                حجز, عايز اجي, احجزلي, موعد
//                - "chat"      → greetings, general questions, anything else

//                Today's date is: {today}
//                Calculate relative dates like "tomorrow", "بكرة", "next Monday"
//                """;
//        }

//        public static string BuildWorkoutPrompt(MemberProfile profile, string userMessage)
//        {
//            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

//            return $$$"""
//                You are a professional fitness trainer.
//                Generate a detailed workout plan for:
//                - Name: {profile.FirstName}
//                - Age: {age}
//                - Weight: {profile.Weight ?? 70}kg
//                - Height: {profile.Height ?? 170}cm
//                - Gender: {profile.Gender}
//                - User Request: {userMessage}

//                Understand the user's goal from their request and generate accordingly.

//                Return ONLY valid JSON, no extra text, no markdown:
//                {{
//                  "name": "...",
//                  "durationWeeks": 4,
//                  "days": [
//                    {{
//                      "dayName": "Monday",
//                      "exercises": [
//                        {{ "name": "...", "sets": 3, "reps": 10, "muscleGroup": "..." }}
//                      ]
//                    }}
//                  ]
//                }}
//                """;
//        }

//        public static string BuildNutritionPrompt(MemberProfile profile, string userMessage)
//        {
//            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

//            return $$$"""
//                You are a certified nutritionist.
//                Generate a nutrition plan for:
//                - Age: {age}
//                - Weight: {profile.Weight ?? 70}kg
//                - Height: {profile.Height ?? 170}cm
//                - Gender: {profile.Gender}
//                - User Request: {userMessage}

//                Understand the user's goal from their request and generate accordingly.

//                Return ONLY valid JSON, no extra text, no markdown:
//                {{
//                  "dailyCalories": 2200,
//                  "proteinGrams": 150,
//                  "carbsGrams": 220,
//                  "fatGrams": 70,
//                  "meals": [
//                    {{
//                      "mealType": "Breakfast",
//                      "name": "...",
//                      "calories": 400,
//                      "proteinGrams": 30,
//                      "carbsGrams": 50,
//                      "fatGrams": 10,
//                      "ingredients": "..."
//                    }}
//                  ]
//                }}
//                """;
//        }

//        public static string BuildChatSystemPrompt(MemberProfile profile)
//        {
//            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

//            return $"""
//                You are Arena's AI fitness assistant.
//                Member info:
//                - Name: {profile.FirstName}
//                - Age: {age}
//                - Weight: {profile.Weight ?? 0}kg
//                - Height: {profile.Height ?? 0}cm
//                - Gender: {profile.Gender}

//                Help with fitness advice, workouts, nutrition, and gym bookings.
//                Be friendly and concise.
//                Reply in the same language the user writes in (English or Arabic).
//                """;
//        }

//        public static string BuildBookingSystemPrompt()
//        {
//            return """
//                You are a gym booking assistant.
//                Extract booking intent and details from the user message.

//                Return ONLY this JSON, no extra text, no markdown:
//                {
//                  "intent": "create" | "reschedule" | "cancel" | "none",
//                  "date": "YYYY-MM-DD or null",
//                  "time": "HH:mm or null",
//                  "message": "friendly reply to user in their language"
//                }
//                """;
//        }
//    }
//}



using ArenaDomain.Entities;

namespace ArenaApplication.AI
{
    public static class PromptBuilder
    {
        public static string BuildIntentDetectionPrompt()
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return $$$"""
                You are an intent detection system for a gym management app called Arena.
                The user may write in English OR Arabic OR both mixed.

                Analyze the user message carefully based on MEANING not exact words.
                Return ONLY this JSON, no extra text, no markdown:
                {{
                  "intent": "workout" | "nutrition" | "booking" | "chat",
                  "date": "YYYY-MM-DD or null",
                  "time": "HH:mm or null"
                }}

                Intent rules:
                - "workout"   → exercise, training, gym program, building muscle,
                                losing weight through exercise,
                                تمرين, برنامج رياضي, عايز اتمرن
                - "nutrition" → food, eating, diet, meals, calories,
                                what to eat, نظام غذائي, اكل, وجبات
                - "booking"   → reserve, schedule, coming to gym, specific time/date,
                                حجز, عايز اجي, احجزلي, موعد
                - "chat"      → greetings, general questions, anything else

                Today's date is: {today}
                Calculate relative dates like "tomorrow", "بكرة", "next Monday"
                """;
        }

        //public static string BuildWorkoutPrompt(MemberProfile profile, string userMessage)
        //{
        //    var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

        //    return $$$"""
        //        You are a professional fitness trainer.
        //        Generate a detailed workout plan for:
        //        - Name: {profile.FirstName}
        //        - Age: {age}
        //        - Weight: {profile.Weight ?? 70}kg
        //        - Height: {profile.Height ?? 170}cm
        //        - Gender: {profile.Gender}
        //        - User Request: {userMessage}

        //        Understand the user's goal from their request and generate accordingly.

        //        Return ONLY valid JSON, no extra text, no markdown:
        //        {{
        //          "name": "...",
        //          "durationWeeks": 4,
        //          "days": [
        //            {{
        //              "dayName": "Monday",
        //              "exercises": [
        //                {{ "name": "...", "sets": 3, "reps": 10, "muscleGroup": "..." }}
        //              ]
        //            }}
        //          ]
        //        }}
        //        """;
        //}

        //public static string BuildWorkoutPrompt(MemberProfile profile, string userMessage)
        //{
        //    var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

        //    return $$$"""
        //You are a professional fitness trainer.
        //Generate a detailed workout plan for:
        //- Name: {profile.FirstName}
        //- Age: {age}
        //- Weight: {profile.Weight ?? 70}kg
        //- Height: {profile.Height ?? 170}cm
        //- Gender: {profile.Gender}
        //- User Request: {userMessage}

        //Understand the user's goal from their request and generate accordingly.

        //IMPORTANT: The plan "name" must be specific to the user's goal.
        //Examples:
        //- Weight loss goal   → "Weight Loss Plan for {profile.FirstName}"
        //- Muscle gain goal   → "Muscle Gain Plan for {profile.FirstName}"
        //- Beginner goal      → "Beginner Fitness Plan for {profile.FirstName}"
        //- Endurance goal     → "Endurance Training Plan for {profile.FirstName}"

        //Return ONLY valid JSON, no extra text, no markdown:
        //{{

        //          "name": "...",
        //  "durationWeeks": 4,
        //  "days": [
        //    {{

        //              "dayName": "Monday",
        //      "exercises": [
        //        {{ "name": "...", "sets": 3, "reps": 10, "muscleGroup": "..." }}
        //      ]
        //    }}
        //  ]
        //}}
        //""";
        //}

        public static string BuildWorkoutPrompt(MemberProfile profile, string userMessage)
        {
            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

            // ✅ Use FirstName or fallback
            var name = string.IsNullOrEmpty(profile.FirstName) ? "User" : profile.FirstName;

            return $$$"""
        You are a professional fitness trainer.
        Generate a detailed workout plan for:
        - Name: {name}
        - Age: {age}
        - Weight: {profile.Weight ?? 70}kg
        - Height: {profile.Height ?? 170}cm
        - Gender: {profile.Gender}
        - User Request: {userMessage}

        IMPORTANT — Understand the goal carefully:
        - "اكسب وزن" or "زيادة وزن" or "gain weight" or "bulk"  → goal is WEIGHT GAIN / MUSCLE GAIN
        - "اخس" or "انحف" or "lose weight" or "fat loss"        → goal is WEIGHT LOSS
        - "عايز اكبر الباي والتراي"                              → goal is ARM MUSCLE GAIN
        - "لياقة" or "fitness" or "fit"                         → goal is GENERAL FITNESS

        The plan "name" MUST reflect the actual goal:
        - Weight Gain  → "Weight Gain Plan for {name}"
        - Weight Loss  → "Weight Loss Plan for {name}"
        - Arm Muscles  → "Arm Muscle Plan for {name}"
        - General      → "Fitness Plan for {name}"

        Return ONLY valid JSON, no extra text, no markdown:
        {{
        
                  "name": "...",
          "durationWeeks": 4,
          "days": [
            {{
        
                      "dayName": "Monday",
              "exercises": [
                {{ "name": "...", "sets": 3, "reps": 10, "muscleGroup": "..." }}
              ]
            }}
          ]
        }}
        """;
        }
   
    //public static string BuildNutritionPrompt(MemberProfile profile, string userMessage)
    //{
    //    var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

    //    return $$$"""
    //        You are a certified nutritionist.
    //        Generate a nutrition plan for:
    //        - Age: {age}
    //        - Weight: {profile.Weight ?? 70}kg
    //        - Height: {profile.Height ?? 170}cm
    //        - Gender: {profile.Gender}
    //        - User Request: {userMessage}

    //        Understand the user's goal from their request and generate accordingly.

    //        Return ONLY valid JSON, no extra text, no markdown:
    //        {{
    //          "dailyCalories": 2200,
    //          "proteinGrams": 150,
    //          "carbsGrams": 220,
    //          "fatGrams": 70,
    //          "meals": [
    //            {{
    //              "mealType": "Breakfast",
    //              "name": "...",
    //              "calories": 400,
    //              "proteinGrams": 30,
    //              "carbsGrams": 50,
    //              "fatGrams": 10,
    //              "ingredients": "..."
    //            }}
    //          ]
    //        }}
    //        """;
    //}

    //        public static string BuildNutritionPrompt(MemberProfile profile, string userMessage)
    //        {
    //            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;
    //            var name = string.IsNullOrEmpty(profile.FirstName) ? "User" : profile.FirstName;

    //            return $$$"""
    //You are a certified professional nutritionist.

    //Generate a personalized nutrition plan for:
    //- Name: {name}
    //- Age: {age}
    //- Weight: {profile.Weight ?? 70}kg
    //- Height: {profile.Height ?? 170}cm
    //- Gender: {profile.Gender}
    //- User Request: {userMessage}

    //IMPORTANT — You MUST understand Arabic and English meaning, not keywords only.

    //Nutrition goal mapping:

    //- "زيادة وزن" / "اكسب وزن" / "تضخيم" / "bulk" / "gain weight"
    //  → High calories, high carbs, high protein (MUSCLE / WEIGHT GAIN)

    //- "خس" / "انقاص وزن" / "دايت" / "fat loss" / "lose weight"
    //  → Low calories, high protein, low carbs (WEIGHT LOSS)

    //- "تنشيف" / "cutting"
    //  → Moderate deficit calories, high protein

    //- "لياقة" / "healthy" / "fit"
    //  → Balanced macros (MAINTENANCE)

    //CRITICAL RULES:
    //- Detect intent from meaning even if mixed Arabic + English
    //- Do NOT ignore Arabic slang or Egyptian dialect
    //- Adjust calories and macros based on goal
    //- Meals must match the goal (not generic)

    //Return ONLY valid JSON, no explanation, no markdown:

    //{{
    //  "goal": "...",
    //  "dailyCalories": 2200,
    //  "proteinGrams": 150,
    //  "carbsGrams": 220,
    //  "fatGrams": 70,
    //  "meals": [
    //    {{
    //      "mealType": "Breakfast",
    //      "name": "...",
    //      "calories": 400,
    //      "proteinGrams": 30,
    //      "carbsGrams": 50,
    //      "fatGrams": 10,
    //      "ingredients": "..."
    //    }}
    //  ]
    //}}
    //""";
    //        }


    //        public static string BuildNutritionPrompt(MemberProfile profile, string userMessage)
    //        {
    //            var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;
    //            var name = string.IsNullOrEmpty(profile.FirstName) ? "User" : profile.FirstName;

    //            return $$$"""
    //You are a certified professional nutritionist.

    //Generate a personalized nutrition plan for:
    //- Name: {name}
    //- Age: {age}
    //- Weight: {profile.Weight ?? 70}kg
    //- Height: {profile.Height ?? 170}cm
    //- Gender: {profile.Gender}
    //- User Request: {userMessage}

    //IMPORTANT:
    //- Detect the language of the user message.
    //- Reply in the SAME language ONLY.
    //- If Arabic → full Arabic response.
    //- If English → full English response.
    //- DO NOT mix languages under any condition.

    //Goal detection:
    //- "زيادة وزن / اكسب وزن / bulk / gain weight" → muscle gain (high calories)
    //- "خس / دايت / lose weight / fat loss" → fat loss (low calories)
    //- "تنشيف / cutting" → lean fat loss
    //- "لياقة / fitness" → maintenance

    //Return ONLY valid JSON:

    //{
    //  "goal": "...",
    //  "dailyCalories": 2200,
    //  "proteinGrams": 150,
    //  "carbsGrams": 220,
    //  "fatGrams": 70,
    //  "meals": [
    //    {
    //      "mealType": "Breakfast",
    //      "name": "...",
    //      "calories": 400,
    //      "proteinGrams": 30,
    //      "carbsGrams": 50,
    //      "fatGrams": 10,
    //      "ingredients": "..."
    //    }
    //  ]
    //}
    //""";
    //        }


    //using ArenaDomain.Entities;

   
            public static string BuildNutritionPrompt(MemberProfile profile, string userMessage)
            {
                var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;
                var name = string.IsNullOrEmpty(profile.FirstName) ? "User" : profile.FirstName;

                return $$$"""
You are a certified professional nutritionist.

Generate a personalized nutrition plan for:
- Name: {name}
- Age: {age}
- Weight: {profile.Weight ?? 70}kg
- Height: {profile.Height ?? 170}cm
- Gender: {profile.Gender}
- User Request: {userMessage}

========================
LANGUAGE RULE:
========================
- Detect user language from message
- If Arabic → respond fully in Arabic
- If English → respond fully in English
- DO NOT mix languages

========================
STRICT OUTPUT RULES:
========================
- Return ONLY valid JSON
- NO text before JSON
- NO text after JSON
- NO markdown
- NO explanation
- First character MUST be { 
- Last character MUST be }

========================
GOAL DETECTION:
========================
- زيادة وزن / gain weight / bulk → high calories
- خس / دايت / fat loss → low calories
- تنشيف / cutting → moderate deficit
- لياقة / fitness → maintenance

========================
RESPONSE FORMAT:
========================

{
  "goal": "string",
  "dailyCalories": 2200,
  "proteinGrams": 150,
  "carbsGrams": 220,
  "fatGrams": 70,
  "meals": [
    {
      "mealType": "Breakfast",
      "name": "string",
      "calories": 400,
      "proteinGrams": 30,
      "carbsGrams": 50,
      "fatGrams": 10,
      "ingredients": "string"
    }
  ]
}
""";
            }


            public static string BuildChatSystemPrompt(MemberProfile profile)
            {
                var age = DateTime.UtcNow.Year - profile.DateOfBirth.Year;

                return $$$"""
                You are Arena's AI fitness assistant.
                Member info:
                - Name: {profile.FirstName}
                - Age: {age}
                - Weight: {profile.Weight ?? 0}kg
                - Height: {profile.Height ?? 0}cm
                - Gender: {profile.Gender}

                Help with fitness advice, workouts, nutrition, and gym bookings.
                Be friendly and concise.
                Reply in the same language the user writes in (English or Arabic).
                """;
            }

            public static string BuildBookingSystemPrompt()
            {
                return """
                You are a gym booking assistant.
                Extract booking intent and details from the user message.

                Return ONLY this JSON, no extra text, no markdown:
                {
                  "intent": "create" | "reschedule" | "cancel" | "none",
                  "date": "YYYY-MM-DD or null",
                  "time": "HH:mm or null",
                  "message": "friendly reply to user in their language"
                }
                """;
            }
        }
   }
