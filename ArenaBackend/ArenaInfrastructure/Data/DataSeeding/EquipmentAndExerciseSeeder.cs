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
                new Equipment { Id = Guid.NewGuid(), Name = "Dumbbells", NameAr = "دمبلز", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Barbell", NameAr = "بار", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Squat Rack", NameAr = "رف السكوات", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Bench", NameAr = "بنش", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Pull-up Bar", NameAr = "عقلة", Category = "Bodyweight", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Cable Machine", NameAr = "جهاز الكابل", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Leg Press Machine", NameAr = "جهاز ضغط الأرجل", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Treadmill", NameAr = "مشاية كهربائية", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Stationary Bike", NameAr = "عجلة رياضية", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.NewGuid(), Name = "Kettlebell", NameAr = "كيتل بيل", Category = "Free Weights", IsAvailable = false } // Example unavailable equipment
            };

            await context.Equipments.AddRangeAsync(equipments);
            await context.SaveChangesAsync();

            // ── Seed Exercises ──────────────────────────────────────────
            var exercises = new List<ExerciseCatalogItem>
            {
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Dumbbell Bench Press", NameAr = "تمرين ضغط الصدر بالدمبلز", MuscleGroup = "Chest", MuscleGroupAr = "الصدر", Description = "Press dumbbells while lying on a bench.", DescriptionAr = "اضغط الدمبلز أثناء الاستلقاء على البنش.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Barbell Squat", NameAr = "سكوات بالبار", MuscleGroup = "Legs", MuscleGroupAr = "الأرجل", Description = "Squat with a barbell across the shoulders.", DescriptionAr = "قم بتمرين القرفصاء مع وضع البار على الكتفين.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Pull-up", NameAr = "عقلة", MuscleGroup = "Back", MuscleGroupAr = "الظهر", Description = "Pull body up to a bar.", DescriptionAr = "اسحب الجسم لأعلى إلى الشريط.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Cable Row", NameAr = "تجديف بالكابل", MuscleGroup = "Back", MuscleGroupAr = "الظهر", Description = "Pull cable towards torso.", DescriptionAr = "اسحب الكابل نحو الجذع.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Leg Press", NameAr = "ضغط الأرجل", MuscleGroup = "Legs", MuscleGroupAr = "الأرجل", Description = "Push weight away with legs.", DescriptionAr = "ادفع الوزن بعيدًا باستخدام الأرجل.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Treadmill Running", NameAr = "الجري على المشاية", MuscleGroup = "Cardio", MuscleGroupAr = "كارديو", Description = "Run on a treadmill.", DescriptionAr = "اجرِ على المشاية الكهربائية.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Stationary Bike Cycling", NameAr = "ركوب الدراجة الثابتة", MuscleGroup = "Cardio", MuscleGroupAr = "كارديو", Description = "Cycle on a stationary bike.", DescriptionAr = "قم بركوب الدراجة الثابتة.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Kettlebell Swing", NameAr = "أرجحة الكيتل بيل", MuscleGroup = "Full Body", MuscleGroupAr = "الجسم بالكامل", Description = "Swing kettlebell between legs and up to chest level.", DescriptionAr = "أرجح الكيتل بيل بين الأرجل وحتى مستوى الصدر.", DifficultyLevel = "Intermediate" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Push-up", NameAr = "الضغط", MuscleGroup = "Chest", MuscleGroupAr = "الصدر", Description = "Push body up from floor.", DescriptionAr = "ادفع الجسم لأعلى من الأرض.", DifficultyLevel = "Beginner" },
                new ExerciseCatalogItem { Id = Guid.NewGuid(), Name = "Bodyweight Squat", NameAr = "سكوات بوزن الجسم", MuscleGroup = "Legs", MuscleGroupAr = "الأرجل", Description = "Squat without weights.", DescriptionAr = "قم بتمرين القرفصاء بدون أوزان.", DifficultyLevel = "Beginner" }
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
