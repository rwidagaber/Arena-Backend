using ArenaDomain.Entities.Gym;
using ArenaDomain.Entities.Workout;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArenaInfrastructure.Data.DataSeeding
{
    public static class EquipmentAndExerciseSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Equipments.AnyAsync() || await context.ExerciseCatalogItems.AnyAsync())
            {
                return; // Already seeded
            }

            // ── Seed Equipment ──────────────────────────────────────────
            var equipments = new List<Equipment>
            {
                new Equipment { Id = Guid.NewGuid(), Name = "Dumbbells", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Barbell", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Squat Rack", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Bench", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Pull-up Bar", Category = "Bodyweight", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Cable Machine", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Leg Press Machine", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Treadmill", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Stationary Bike", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Kettlebell", Category = "Free Weights", IsAvailable = false } // Example unavailable equipment
            };

            await context.Equipments.AddRangeAsync(equipments);
            await context.SaveChangesAsync();

            // ── Seed Exercises ──────────────────────────────────────────
            var exercises = new List<ExerciseCatalogItem>
            {
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Dumbbell Bench Press", MuscleGroup = "Chest", Description = "Press dumbbells while lying on a bench.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Barbell Squat", MuscleGroup = "Legs", Description = "Squat with a barbell across the shoulders.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Pull-up", MuscleGroup = "Back", Description = "Pull body up to a bar.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Cable Row", MuscleGroup = "Back", Description = "Pull cable towards torso.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Leg Press", MuscleGroup = "Legs", Description = "Push weight away with legs.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Treadmill Running", MuscleGroup = "Cardio", Description = "Run on a treadmill.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Stationary Bike Cycling", MuscleGroup = "Cardio", Description = "Cycle on a stationary bike.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Kettlebell Swing", MuscleGroup = "Full Body", Description = "Swing kettlebell between legs and up to chest level.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Push-up", MuscleGroup = "Chest", Description = "Push body up from floor.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Bodyweight Squat", MuscleGroup = "Legs", Description = "Squat without weights.", DifficultyLevel = "Beginner" }
            };

            await context.ExerciseCatalogItems.AddRangeAsync(exercises);
            await context.SaveChangesAsync();

            // ── Seed Exercise-Equipment Requirements ─────────────────────
            var eqDict = equipments.ToDictionary(e => e.Name, e => e.Id);
            var exDict = exercises.ToDictionary(e => e.Name, e => e.Id);

            var requirements = new List<ExerciseEquipmentRequirement>
            {
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Dumbbell Bench Press"], EquipmentId = eqDict["Dumbbells"] },
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Dumbbell Bench Press"], EquipmentId = eqDict["Bench"] },
                
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Barbell Squat"], EquipmentId = eqDict["Barbell"] },
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Barbell Squat"], EquipmentId = eqDict["Squat Rack"] },
                
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Pull-up"], EquipmentId = eqDict["Pull-up Bar"] },
                
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Cable Row"], EquipmentId = eqDict["Cable Machine"] },
                
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Leg Press"], EquipmentId = eqDict["Leg Press Machine"] },
                
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Treadmill Running"], EquipmentId = eqDict["Treadmill"] },
                
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Stationary Bike Cycling"], EquipmentId = eqDict["Stationary Bike"] },
                
                new ExerciseEquipmentRequirement { Id = Guid.NewGuid(), ExerciseCatalogItemId = exDict["Kettlebell Swing"], EquipmentId = eqDict["Kettlebell"] }
                // Push-up and Bodyweight Squat don't require specific equipment, so we omit them here.
            };

            await context.ExerciseEquipmentRequirements.AddRangeAsync(requirements);
            await context.SaveChangesAsync();
        }
    }
}
