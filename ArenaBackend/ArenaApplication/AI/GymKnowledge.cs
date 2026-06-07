using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.AI
{
    public static class GymKnowledge
    {
        public static string GetExerciseGuide(string goal, string experience) => $"""
        === EXERCISE GUIDELINES ===
        
        For {goal} at {experience} level:
        
        BEGINNER RULES:
        - Start with compound movements
        - 3 days per week maximum
        - 2-3 sets per exercise
        - Focus on form over weight
        - Rest 60-90 seconds between sets
        
        INTERMEDIATE RULES:
        - 4 days per week
        - Progressive overload every week
        - 3-4 sets per exercise
        - Mix compound and isolation
        
        WEIGHT LOSS SPECIFIC:
        - Caloric deficit 300-500 calories
        - High reps (12-15) with moderate weight
        - Include cardio 3x per week
        - HIIT recommended
        
        MUSCLE GAIN SPECIFIC:
        - Caloric surplus 200-300 calories
        - Low reps (6-10) with heavy weight
        - Progressive overload mandatory
        - Protein 1.6-2.2g per kg bodyweight
        """;

        public static string GetNutritionGuide(string goal, decimal weight) => $"""
        === NUTRITION GUIDELINES ===
        
        For {weight}kg person with {goal}:
        
        PROTEIN TARGETS:
        - Weight Loss: {weight * 2.0m}g per day
        - Muscle Gain: {weight * 2.2m}g per day
        - Maintenance: {weight * 1.6m}g per day
        
        CALORIE CALCULATION (Mifflin-St Jeor):
        - Calculate BMR first
        - Multiply by activity factor
        - Add/subtract based on goal
        
        MEAL TIMING:
        - Pre-workout: Carbs + Protein 1-2hr before
        - Post-workout: Protein within 30 min
        - Before bed: Slow protein (casein/eggs)
        
        HYDRATION:
        - Minimum: {weight * 0.033m:F1}L per day
        - During workout: 500ml per hour
        """;

        public static string GetInjuryGuide(string injuries) => $"""
        === INJURY MODIFICATIONS ===
        
        Detected injuries: {injuries}
        
        KNEE INJURY:
        - Avoid: Deep squats, jumping, running
        - Replace with: Leg press, swimming, cycling
        - Focus on: Quad strengthening, flexibility
        
        LOWER BACK:
        - Avoid: Deadlifts, heavy squats, sit-ups
        - Replace with: Romanian deadlifts, planks, bird dogs
        - Focus on: Core stability
        
        SHOULDER:
        - Avoid: Overhead press, upright rows
        - Replace with: Cable flyes, dumbbell lateral raises
        - Focus on: Rotator cuff strengthening
        """;
    }
}
