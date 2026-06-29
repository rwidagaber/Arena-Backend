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
            // 1. Delete test equipment
            var testEquipments = await context.Equipments
                .Where(e => e.Name == "eqtest" || e.Name == "testing" || e.Category == "test" || e.Category == "تجريبي")
                .ToListAsync();

            if (testEquipments.Any())
            {
                var testEquipIds = testEquipments.Select(te => te.Id).ToList();
                var associatedReqs = await context.ExerciseEquipmentRequirements
                    .Where(r => testEquipIds.Contains(r.EquipmentId))
                    .ToListAsync();
                if (associatedReqs.Any())
                {
                    context.ExerciseEquipmentRequirements.RemoveRange(associatedReqs);
                }
                context.Equipments.RemoveRange(testEquipments);
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seeder] Deleted {testEquipments.Count} test equipment records.");
            }

            // 2. Define expected professional equipment list (30 items)
            var targetEquipments = new List<Equipment>
            {
                new Equipment { Id = Guid.Parse("FBDCD689-292F-4559-AE52-66CACE391E73"), Name = "Dumbbells", NameAr = "دمبلز", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.Parse("DCADCF2D-76CF-455B-9488-4E19FE68B2DA"), Name = "Barbell", NameAr = "بار الحديد", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.Parse("A62D9450-CD51-4A16-B6EA-325338145AFD"), Name = "Bench", NameAr = "بنش مستوٍ", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.Parse("6A34FF46-0B3D-4F7A-8F65-AFF9113933B5"), Name = "Squat Rack", NameAr = "حامل السكوات", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("AA6CA8B6-1A7A-479B-BCC9-91E3D152A9BE"), Name = "Cable Machine", NameAr = "جهاز الكابل", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("AAC80680-787C-47D5-A1C3-B83EAAF76C12"), Name = "Leg Press Machine", NameAr = "جهاز دفع الأرجل", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("BC993E4F-3517-4679-BD87-95B88D8F2675"), Name = "Treadmill", NameAr = "مشاية كهربائية", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.Parse("B244F0D7-C7F9-42D6-AE3A-DC5925AD0261"), Name = "Stationary Bike", NameAr = "دراجة ثابتة", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.Parse("04D53FB2-D639-46F0-8987-D4FF070A2593"), Name = "Kettlebell", NameAr = "كيتل بيل", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.Parse("A7E03BD6-80A6-489D-BAA9-E1352B42D80F"), Name = "Pull-up Bar", NameAr = "شريط العقلة", Category = "Bodyweight", IsAvailable = true },
                new Equipment { Id = Guid.Parse("E8A81144-884A-485D-AC82-2C5E78DCA3F2"), Name = "Smith Machine", NameAr = "جهاز سميث", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("3BB6783B-D5A8-4D2A-83B1-9988C78FA25D"), Name = "Lat Pulldown Machine", NameAr = "جهاز السحب العالي للظهر", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("8F8BC95B-883F-44FA-BA2F-D0308C99CEE2"), Name = "Leg Extension Machine", NameAr = "جهاز رفرفة أرجل أمامي", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("2D0E6A2B-F730-4C90-8EA2-3F95C48A14E1"), Name = "Leg Curl Machine", NameAr = "جهاز رفرفة أرجل خلفي", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("5C71AE8F-BDCA-4E38-AA03-518DCA0FFCE9"), Name = "Pec Deck Machine", NameAr = "جهاز الفراشة للصدر", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("6D9EAA0A-5A1D-4952-B582-F8D279E11F24"), Name = "Elliptical Trainer", NameAr = "جهاز الأوربتراك", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.Parse("9A8CBEF8-BD03-45AF-A281-7DC4C9AE5E33"), Name = "Dips Bar", NameAr = "جهاز المتوازي", Category = "Bodyweight", IsAvailable = true },
                new Equipment { Id = Guid.Parse("BC87114A-C9E1-4DAF-83B2-DF8A9CE40AA2"), Name = "Rowing Machine", NameAr = "جهاز التجديف", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.Parse("11B7C163-7182-429D-83A1-C182B12F073C"), Name = "EZ Bar", NameAr = "بار متعرج EZ", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.Parse("22B8C174-8193-439E-93A2-C293C23D084D"), Name = "Preacher Curl Bench", NameAr = "دكة بايسبس لاري", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.Parse("33B9C185-91A4-44AF-A3B3-C3A4D34E095E"), Name = "Calf Raise Machine", NameAr = "جهاز الساق / بطات", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("44BAC196-A1B5-45B0-B3C4-C4B5E45F0A6F"), Name = "Cable Crossover Machine", NameAr = "جهاز الكابل المزدوج", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("55BBC1A7-B1C6-46C1-C3C5-C5C6F56A0B7F"), Name = "Abdominal Crunch Machine", NameAr = "جهاز بطن", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("66BCC1B8-C1D7-47C2-D3C6-C6D7067B0C8F"), Name = "Assisted Pull-up / Dip Machine", NameAr = "جهاز العقلة والمتوازي المساعد", Category = "Strength Machine", IsAvailable = true },
                new Equipment { Id = Guid.Parse("77BDC1C9-D1E8-48D3-E3D7-C7D8078C0D9F"), Name = "Medicine Ball", NameAr = "كرة طبية", Category = "Free Weights", IsAvailable = true },
                new Equipment { Id = Guid.Parse("88BEC1DA-E1F9-49E4-F3D8-C8D9079D0EAF"), Name = "Battle Ropes", NameAr = "حبال المقاومة الثقيلة", Category = "Cardio", IsAvailable = true },
                new Equipment { Id = Guid.Parse("99BFC1EB-F20A-4AF5-A3E9-C9DA08AE0FBF"), Name = "Hyperextension Bench", NameAr = "دكة قطنية", Category = "Bodyweight", IsAvailable = true },
                new Equipment { Id = Guid.Parse("AABFC2FC-031B-4BF6-B3FA-DAEA09BF1FCF"), Name = "Foam Roller", NameAr = "فوم رولر", Category = "Flexibility", IsAvailable = true },
                new Equipment { Id = Guid.Parse("BBBFC3FD-142C-4CF7-C3FB-DBEB0ABF2FDF"), Name = "TRXs / Suspension Trainer", NameAr = "حبال تي آر إكس", Category = "Bodyweight", IsAvailable = true },
                new Equipment { Id = Guid.Parse("CCCFC4FE-253D-4DF8-D3FC-DCEC0CFA3FEF"), Name = "Yoga Mat", NameAr = "سجادة يوجا", Category = "Flexibility", IsAvailable = true }
            };

            // 3. Upsert Equipments
            var existingEquipments = await context.Equipments.ToListAsync();
            var equipmentMap = existingEquipments.ToDictionary(e => e.Id);

            foreach (var target in targetEquipments)
            {
                if (equipmentMap.TryGetValue(target.Id, out var existing))
                {
                    existing.Name = target.Name;
                    existing.NameAr = target.NameAr;
                    existing.Category = target.Category;
                    existing.IsAvailable = target.IsAvailable;
                    existing.UpdatedAt = DateTime.UtcNow;
                    context.Equipments.Update(existing);
                }
                else
                {
                    var existingByName = existingEquipments.FirstOrDefault(e => e.Name.Equals(target.Name, StringComparison.OrdinalIgnoreCase));
                    if (existingByName != null)
                    {
                        existingByName.Name = target.Name;
                        existingByName.NameAr = target.NameAr;
                        existingByName.Category = target.Category;
                        existingByName.IsAvailable = target.IsAvailable;
                        existingByName.UpdatedAt = DateTime.UtcNow;
                        context.Equipments.Update(existingByName);
                        target.Id = existingByName.Id;
                    }
                    else
                    {
                        target.CreatedAt = DateTime.UtcNow;
                        await context.Equipments.AddAsync(target);
                    }
                }
            }
            await context.SaveChangesAsync();

            // 4. Define expected professional exercises list (50 items)
            var targetExercises = new List<ExerciseCatalogItem>
            {
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("742C436A-0CE7-4C1C-B7DE-0D39F6C06196"),
                    Name = "Dumbbell Bench Press",
                    NameAr = "تمرين ضغط الصدر بالدمبلز",
                    Description = "Press dumbbells while lying on a flat bench.",
                    DescriptionAr = "اضغط الدمبلز لأعلى أثناء الاستلقاء على بنش مستوٍ.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A82EC782-1881-4039-B938-23D3E7FA9632"),
                    Name = "Kettlebell Swing",
                    NameAr = "أرجحة الكيتل بيل",
                    Description = "Swing kettlebell between legs and up to chest level.",
                    DescriptionAr = "أرجح الكيتل بيل بين الأرجل وحتى مستوى الصدر.",
                    MuscleGroup = "Full Body",
                    MuscleGroupAr = "الجسم بالكامل",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("13985DE4-9152-4765-9EF9-31F62AD89758"),
                    Name = "Barbell Squat",
                    NameAr = "سكوات بالبار",
                    Description = "Squat with a barbell across the shoulders.",
                    DescriptionAr = "تمرين القرفصاء مع وضع بار الحديد على الكتفين.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("E6541941-5D81-4C2D-B9FD-3C3E00C2827F"),
                    Name = "Pull-up",
                    NameAr = "تمرين العقلة",
                    Description = "Pull body up to a bar.",
                    DescriptionAr = "اسحب جسمك لأعلى حتى يتجاوز ذقنك شريط العقلة.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("D08D0293-CB1B-4589-9757-3DC0998DA76F"),
                    Name = "Push-up",
                    NameAr = "تمرين الضغط",
                    Description = "Push body up from floor.",
                    DescriptionAr = "ادفع جسمك لأعلى من الأرض مع الحفاظ على استقامة ظهرك.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("F33E0CA3-132A-4A95-A4E6-636913485BDD"),
                    Name = "Cable Row",
                    NameAr = "تجديف بالكابل",
                    Description = "Pull cable towards torso while seated.",
                    DescriptionAr = "اسحب مقبض الكابل نحو الجذع أثناء الجلوس.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("ECDC9C54-46BD-45FF-8290-77B7B39FCF36"),
                    Name = "Treadmill Running",
                    NameAr = "الجري على المشاية الكهربائية",
                    Description = "Run on a treadmill.",
                    DescriptionAr = "الركض أو المشي السريع على المشاية الكهربائية.",
                    MuscleGroup = "Cardio",
                    MuscleGroupAr = "كارديو",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("0DC48489-F9F1-40FB-9FBC-86DE76604EEC"),
                    Name = "Bodyweight Squat",
                    NameAr = "سكوات بوزن الجسم",
                    Description = "Squat without weights.",
                    DescriptionAr = "الهبوط بوضعية القرفصاء باستخدام وزن الجسم فقط.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("8CAC8C0C-E295-4E4B-B0C0-906A4DEDA763"),
                    Name = "Leg Press",
                    NameAr = "تمرين ضغط الأرجل",
                    Description = "Push weight plate away with legs.",
                    DescriptionAr = "ادفع منصة الأوزان بعيداً باستخدام الأرجل.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("4B2BCAFF-D352-4B2E-B350-9392B44217BF"),
                    Name = "Stationary Bike Cycling",
                    NameAr = "ركوب الدراجة الثابتة",
                    Description = "Cycle on a stationary bike.",
                    DescriptionAr = "تبديل الدراجة الثابتة لتحسين اللياقة البدنية وحرق السعرات.",
                    MuscleGroup = "Cardio",
                    MuscleGroupAr = "كارديو",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("98EA2C11-1AA5-4A3C-9EBD-C88AA8DFE2E2"),
                    Name = "Lat Pulldown",
                    NameAr = "سحب عالي للظهر",
                    Description = "Pull the bar down towards your chest.",
                    DescriptionAr = "اسحب البار لأسفل باتجاه أعلى الصدر لتقوية عضلات الظهر العريضة.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("DCA29A7F-78A1-4B9F-83B0-D1FA8BE39DC3"),
                    Name = "Smith Machine Incline Bench Press",
                    NameAr = "ضغط بنش مائل على جهاز سميث",
                    Description = "Press the bar upward on an incline bench using the Smith machine.",
                    DescriptionAr = "اضغط البار لأعلى على بنش مائل باستخدام جهاز سميث.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("3D5C8A7E-C2D9-48A1-A1E2-F4A3928AD0CF"),
                    Name = "Leg Extension",
                    NameAr = "تمديد الأرجل على الجهاز",
                    Description = "Extend your legs outward against the machine's pad.",
                    DescriptionAr = "قم بفرد رجليك بالكامل ضد منصة المقاومة لتقوية عضلات الفخذ الأمامية.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("BDC87E2C-F9D1-4E8A-9A82-C39F4E7BDCF4"),
                    Name = "Lying Leg Curl",
                    NameAr = "ثني الأرجل مستلقياً",
                    Description = "Curl legs upward towards the glutes while lying face down.",
                    DescriptionAr = "قم بثني رجليك لأعلى باتجاه الأرداف أثناء الاستلقاء على البطن.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("4CA2E198-BC73-4A1E-B4A1-E839A2FA8CE2"),
                    Name = "Pec Deck Fly",
                    NameAr = "تجميع الصدر على جهاز الفراشة",
                    Description = "Bring the machine pads together in front of your chest.",
                    DescriptionAr = "ضم ذراعي الجهاز معاً أمام صدرك لتركيز الجهد على عضلات الصدر.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("BC9E8A7D-DCBA-48A1-B9D2-E40FA2B39EFE"),
                    Name = "Tricep Pushdown",
                    NameAr = "سحب ترايسبس بالكابل",
                    Description = "Push the cable attachment down until arms are fully extended.",
                    DescriptionAr = "ادفع ملحق الكابل لأسفل حتى فرد الذراعين بالكامل لتقوية الترايسبس.",
                    MuscleGroup = "Arms",
                    MuscleGroupAr = "الذراعين",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("5DA82C3D-E4A1-4B9E-B3A9-F2EAD8CF9C3C"),
                    Name = "Dumbbell Bicep Curl",
                    NameAr = "تبادل بايسبس بالدمبلز",
                    Description = "Curl the dumbbells upward toward your shoulders.",
                    DescriptionAr = "اثنِ الكوع لرفع الدمبلز نحو الكتفين لتقوية عضلة البايسبس.",
                    MuscleGroup = "Arms",
                    MuscleGroupAr = "الذراعين",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("7DA8E2BC-839A-48FA-A7E1-CD8FFA3E9E2C"),
                    Name = "Barbell Deadlift",
                    NameAr = "الرفعة المميتة بالبار",
                    Description = "Lift the barbell from the floor to hip level with a straight back.",
                    DescriptionAr = "ارفع البار من الأرض حتى مستوى الورك مع الحفاظ على استقامة الظهر.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Advanced"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("8DA29C8B-FA73-4A82-9BCA-ECBA3DFF9C1E"),
                    Name = "Chest Dips",
                    NameAr = "غطس الصدر على المتوازي",
                    Description = "Lower and raise your body on parallel bars.",
                    DescriptionAr = "اخفض وارفع جسمك على قضبان المتوازي لتقوية الصدر والترايسبس.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("ACDE9B8F-87A2-4BCA-8CDE-FEA9D1C06EFE"),
                    Name = "Rowing Machine Training",
                    NameAr = "التدريب على جهاز التجديف",
                    Description = "Perform rowing motion on the machine.",
                    DescriptionAr = "أداء حركة التجديف على جهاز التجديف لتقوية الظهر والقلب.",
                    MuscleGroup = "Cardio",
                    MuscleGroupAr = "كارديو",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A1B7C163-7182-429D-83A1-C182B12F073C"),
                    Name = "EZ-Bar Preacher Curl",
                    NameAr = "تبادل بايسبس بار متعرج على الدكة",
                    Description = "Perform bicep curls using an EZ bar on a preacher bench.",
                    DescriptionAr = "قم بثني البايسبس باستخدام بار متعرج على دكة الواعظ (لاري).",
                    MuscleGroup = "Arms",
                    MuscleGroupAr = "الذراعين",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A2B8C174-8193-439E-93A2-C293C23D084D"),
                    Name = "Standing Calf Raise",
                    NameAr = "تمرين بطات واقفاً على الجهاز",
                    Description = "Raise your heels while standing with weight on shoulders.",
                    DescriptionAr = "ارفع كعبيك أثناء الوقوف على جهاز ربلة الساق لتقوية عضلات الساق.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A3B9C185-91A4-44AF-A3B3-C3A4D34E095E"),
                    Name = "Cable Crossover Chest Fly",
                    NameAr = "تجميع الصدر كابل كروس",
                    Description = "Pull cables in a downward/forward arc to target chest chest fly.",
                    DescriptionAr = "ضم مقابض الكابلات معاً أمام أسفل صدرك لبناء عضلات الصدر الجانبية.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A4BAC196-A1B5-45B0-B3C4-C4B5E45F0A6F"),
                    Name = "Machine Crunch",
                    NameAr = "طحن البطن على الجهاز",
                    Description = "Perform abdominal crunches using a weighted machine.",
                    DescriptionAr = "قم بطحن عضلات البطن ضد المقاومة باستخدام جهاز البطن المخصص.",
                    MuscleGroup = "Core",
                    MuscleGroupAr = "البطن والوسط",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A5BBC1A7-B1C6-46C1-C3C5-C5C6F56A0B7F"),
                    Name = "Assisted Pull-up",
                    NameAr = "تمرين عقلة مساعد",
                    Description = "Perform pull-ups with counterweight assistance.",
                    DescriptionAr = "قم بأداء تمرين العقلة بمساعدة الوزن المعاكس لتخفيف وزن الجسم.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A6BCC1B8-C1D7-47C2-D3C6-C6D7067B0C8F"),
                    Name = "Assisted Chest Dip",
                    NameAr = "تمرين متوازي مساعد",
                    Description = "Perform dips with counterweight assistance.",
                    DescriptionAr = "تمرن على جهاز المتوازي بمساعدة الوزن المعاكس لتسهيل الحركة.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A7BDC1C9-D1E8-48D3-E3D7-C7D8078C0D9F"),
                    Name = "Medicine Ball Russian Twist",
                    NameAr = "التواء روسي بالكرة الطبية",
                    Description = "Rotate your torso side-to-side holding a medicine ball.",
                    DescriptionAr = "قم بتدوير الجذع من جانب لآخر أثناء الجلوس مع مسك الكرة الطبية لتقوية الخواصر.",
                    MuscleGroup = "Core",
                    MuscleGroupAr = "البطن والوسط",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A8BEC1DA-E1F9-49E4-F3D8-C8D9079D0EAF"),
                    Name = "Battle Ropes Waves",
                    NameAr = "موجات حبل المقاومة",
                    Description = "Perform wave motion with heavy battle ropes.",
                    DescriptionAr = "حرك حبال المقاومة الثقيلة لإنشاء موجات متتالية لتحسين اللياقة والقدرة الانفجارية.",
                    MuscleGroup = "Cardio",
                    MuscleGroupAr = "كارديو",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("A9BFC1EB-F20A-4AF5-A3E9-C9DA08AE0FBF"),
                    Name = "Hyperextension",
                    NameAr = "تمرين القطنية على الدكة",
                    Description = "Lower and raise your upper body on a hyperextension bench.",
                    DescriptionAr = "اخفض وارفع جذعك على دكة القطنية لتقوية أسفل الظهر والأرداف.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B0BFC2FC-031B-4BF6-B3FA-DAEA09BF1FCF"),
                    Name = "Foam Roller IT Band Roll",
                    NameAr = "مساج الفخذ بالرولر",
                    Description = "Use a foam roller to massage the outer thigh (IT band).",
                    DescriptionAr = "استخدم أسطوانة الفوم (الرولر) لتدليك الفخذ الخارجي وتخفيف الشد العضلي.",
                    MuscleGroup = "Flexibility",
                    MuscleGroupAr = "المرونة والاستشفاء",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B1BFC3FD-142C-4CF7-C3FB-DBEB0ABF2FDF"),
                    Name = "TRX Suspension Row",
                    NameAr = "سحب جسم تي آر إكس",
                    Description = "Perform rows using a suspension trainer.",
                    DescriptionAr = "قم بأداء تمرين السحب المائل باستخدام حبال الـ TRX المعلقة.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B2BFC4FE-253D-4DF8-D3FC-DCEC0CFA3FEF"),
                    Name = "Yoga Mat Plank",
                    NameAr = "تمرين البلانك على السجادة",
                    Description = "Hold a pushup position on elbows for core stability.",
                    DescriptionAr = "ثبّت جسمك على المرفقين وأطراف الأصابع لتقوية عضلات البطن والعمود الفقري.",
                    MuscleGroup = "Core",
                    MuscleGroupAr = "البطن والوسط",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B3BFC5FF-364E-4EF9-E3FD-DDFD0DFB4FFF"),
                    Name = "Dumbbell Incline Bench Press",
                    NameAr = "ضغط الصدر بالدمبلز على بنش مائل",
                    Description = "Press dumbbells upward while lying on an incline bench.",
                    DescriptionAr = "اضغط الدمبلز لأعلى أثناء الاستلقاء على بنش مائل لاستهداف عضلات الصدر العلوي.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B4BFC6F0-475F-4F0A-F3FE-EEFE0EFC5F0F"),
                    Name = "Dumbbell Fly",
                    NameAr = "تجميع الصدر بالدمبلز مستوٍ",
                    Description = "Perform chest flies using dumbbells on a flat bench.",
                    DescriptionAr = "قم بتفتيح وتجميع الصدر بالدمبلز على بنش مستوٍ لعزل عضلات الصدر.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B5BFC7F1-5860-4F1B-03FF-FFFFAFDF6F1F"),
                    Name = "Barbell Bench Press",
                    NameAr = "تمرين بنش برس بالبار",
                    Description = "Press a barbell while lying on a flat bench.",
                    DescriptionAr = "اضغط البار لأعلى أثناء الاستلقاء على البنش المستوي لتقوية عضلات الصدر الكبرى.",
                    MuscleGroup = "Chest",
                    MuscleGroupAr = "الصدر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B6BFC8F2-6971-4F2B-14FF-000000000000"),
                    Name = "Dumbbell Shoulder Press",
                    NameAr = "ضغط الأكتاف بالدمبلز جالساً",
                    Description = "Press dumbbells overhead while seated on a bench.",
                    DescriptionAr = "اضغط الدمبلز لأعلى فوق الرأس أثناء الجلوس لتقوية عضلات الأكتاف.",
                    MuscleGroup = "Shoulders",
                    MuscleGroupAr = "الأكتاف",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B7BFC9F3-7A82-4F3B-25FF-111111111111"),
                    Name = "Dumbbell Lateral Raise",
                    NameAr = "رفرفة أكتاف جانبي بالدمبلز",
                    Description = "Raise dumbbells out to the sides to target side delts.",
                    DescriptionAr = "ارفع الدمبلز جانباً حتى مستوى الكتفين لتقوية عضلات الأكتاف الجانبية.",
                    MuscleGroup = "Shoulders",
                    MuscleGroupAr = "الأكتاف",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B8BFCAF4-8B93-4F4B-36FF-222222222222"),
                    Name = "Barbell Overhead Press",
                    NameAr = "ضغط عسكري بالبار واقفاً",
                    Description = "Press a barbell overhead while standing.",
                    DescriptionAr = "اضغط بار الحديد لأعلى فوق الرأس من وضع الوقوف لبناء القوة العامة للأكتاف والجسم.",
                    MuscleGroup = "Shoulders",
                    MuscleGroupAr = "الأكتاف",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("B9BFCBF5-9CA4-4F5B-47FF-333333333333"),
                    Name = "Face Pull",
                    NameAr = "سحب الوجه بالكابل",
                    Description = "Pull cable towards face to target rear delts and upper back.",
                    DescriptionAr = "اسحب الكابل باتجاه الوجه مع تفتيح المرفقين لتقوية الأكتاف الخلفية وأعلى الظهر.",
                    MuscleGroup = "Shoulders",
                    MuscleGroupAr = "الأكتاف",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C0BFCCF6-0DB5-4F6B-58FF-444444444444"),
                    Name = "Dumbbell Hammer Curl",
                    NameAr = "تبادل بايسبس مطرقة بالدمبلز",
                    Description = "Perform bicep curls with a neutral grip.",
                    DescriptionAr = "اثنِ الذراع بالدمبل مع إبقاء راحة اليد متواجهة (قبضة المطرقة) لتقوية الساعد والبايسبس.",
                    MuscleGroup = "Arms",
                    MuscleGroupAr = "الذراعين",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C1BFCDF7-1EC6-4F7B-69FF-555555555555"),
                    Name = "Overhead Dumbbell Tricep Extension",
                    NameAr = "تمديد ترايسبس بالدمبل خلف الرأس",
                    Description = "Extend dumbbell overhead to target the triceps.",
                    DescriptionAr = "ارفع الدمبل بكلتا اليدين ثم اخفضه خلف رأسك وافرده للأعلى لتقوية الترايسبس.",
                    MuscleGroup = "Arms",
                    MuscleGroupAr = "الذراعين",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C2BFCEF8-2FD7-4F8B-7AFF-666666666666"),
                    Name = "Cable Tricep Overhead Extension",
                    NameAr = "تمديد ترايسبس كابل خلف الرأس",
                    Description = "Extend cable attachment overhead while facing away from the machine.",
                    DescriptionAr = "اسحب الكابل من خلف رأسك للأمام أثناء الانحناء قليلاً لتفجير عضلات الترايسبس.",
                    MuscleGroup = "Arms",
                    MuscleGroupAr = "الذراعين",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C3BFCFF9-30E8-4F9B-8BFF-777777777777"),
                    Name = "Barbell Bent-Over Row",
                    NameAr = "تجديف بالبار للظهر",
                    Description = "Pull barbell towards abdomen while bending forward.",
                    DescriptionAr = "انحنِ للأمام واسحب البار نحو أسفل البطن لتقوية عضلات الظهر وسماكتها.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C4BFC0A0-41F9-4FAB-9CFF-888888888888"),
                    Name = "Dumbbell One-Arm Row",
                    NameAr = "منشار بالدمبل للظهر",
                    Description = "Pull dumbbell towards hip with one arm on a bench.",
                    DescriptionAr = "ارتكز بركبتك ويدك على البنش واسحب الدمبل بذراع واحدة نحو وركك لتقوية اللاتس.",
                    MuscleGroup = "Back",
                    MuscleGroupAr = "الظهر",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C5BFC0B1-520A-4FBA-ADFF-999999999999"),
                    Name = "Bulgarian Split Squat",
                    NameAr = "سكوات بلغاري بالدمبل",
                    Description = "Perform single-leg squats with rear foot elevated on a bench.",
                    DescriptionAr = "ضع قدماً واحدة على البنش بالخلف واهبط بالأخرى لتقوية الأفخاذ الأمامية والأرداف بشكل مكثف.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C6BFC0C2-631B-4FCB-BEFF-AAAAAAAAAAAA"),
                    Name = "Dumbbell Romanian Deadlift",
                    NameAr = "رفعة مميتة رومانية بالدمبلز",
                    Description = "Perform Romanian deadlifts holding dumbbells.",
                    DescriptionAr = "انحنِ للأمام مع إبقاء الظهر مستقيماً والركبتين شبه مفرودتين لتشغيل عضلات الفخذ الخلفية والقطنية.",
                    MuscleGroup = "Legs",
                    MuscleGroupAr = "الأرجل",
                    DifficultyLevel = "Intermediate"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C7BFC0D3-742C-4FDB-CFFF-BBBBBBBBBBBB"),
                    Name = "Lying Leg Raise",
                    NameAr = "رفع الأرجل مستلقياً للبطن",
                    Description = "Raise your legs from the floor while lying flat.",
                    DescriptionAr = "استلقِ على ظهرك تماماً وارفع رجليك للأعلى ببطء لاستهداف عضلات أسفل البطن.",
                    MuscleGroup = "Core",
                    MuscleGroupAr = "البطن والوسط",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C8BFC0E4-853D-4FEB-DFFF-CCCCCCCCCCCC"),
                    Name = "Bicycle Crunch",
                    NameAr = "طحن البطن بالتبادل",
                    Description = "Perform crunches alternating elbows to opposite knees.",
                    DescriptionAr = "حرك قدميك كالدراجة مع لمس الكوع الأيمن للركبة اليسرى بالتبادل لتشغيل عضلات البطن والخواصر.",
                    MuscleGroup = "Core",
                    MuscleGroupAr = "البطن والوسط",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("C9BFC0F5-964E-4FFB-EFFF-DDDDDDDDDDDD"),
                    Name = "Mountain Climbers",
                    NameAr = "تمرين تسلق الجبل",
                    Description = "Drive knees to chest rapidly from a plank position.",
                    DescriptionAr = "من وضعية البلانك، ادفع ركبتيك نحو صدرك بالتناوب بسرعة عالية لتنشيط عضلات البطن وحرق السعرات.",
                    MuscleGroup = "Cardio",
                    MuscleGroupAr = "كارديو",
                    DifficultyLevel = "Beginner"
                },
                new ExerciseCatalogItem
                {
                    Id = Guid.Parse("D0BFC006-A75F-400C-FFFF-EEEEEEEEEEEE"),
                    Name = "Elliptical Cross Training",
                    NameAr = "التدريب على جهاز الأوربتراك",
                    Description = "Exercise on the elliptical cross trainer.",
                    DescriptionAr = "تمرن على جهاز الأوربتراك (Cross Trainer) لكارديو متكامل للجسم ومنخفض التأثير على المفاصل.",
                    MuscleGroup = "Cardio",
                    MuscleGroupAr = "كارديو",
                    DifficultyLevel = "Beginner"
                }
            };

            // 5. Upsert Exercises
            var existingExercises = await context.ExerciseCatalogItems.ToListAsync();
            var exerciseMap = existingExercises.ToDictionary(e => e.Id);

            foreach (var target in targetExercises)
            {
                if (exerciseMap.TryGetValue(target.Id, out var existing))
                {
                    existing.Name = target.Name;
                    existing.NameAr = target.NameAr;
                    existing.Description = target.Description;
                    existing.DescriptionAr = target.DescriptionAr;
                    existing.MuscleGroup = target.MuscleGroup;
                    existing.MuscleGroupAr = target.MuscleGroupAr;
                    existing.DifficultyLevel = target.DifficultyLevel;
                    existing.UpdatedAt = DateTime.UtcNow;
                    context.ExerciseCatalogItems.Update(existing);
                }
                else
                {
                    var existingByName = existingExercises.FirstOrDefault(e => e.Name.Equals(target.Name, StringComparison.OrdinalIgnoreCase));
                    if (existingByName != null)
                    {
                        existingByName.Name = target.Name;
                        existingByName.NameAr = target.NameAr;
                        existingByName.Description = target.Description;
                        existingByName.DescriptionAr = target.DescriptionAr;
                        existingByName.MuscleGroup = target.MuscleGroup;
                        existingByName.MuscleGroupAr = target.MuscleGroupAr;
                        existingByName.DifficultyLevel = target.DifficultyLevel;
                        existingByName.UpdatedAt = DateTime.UtcNow;
                        context.ExerciseCatalogItems.Update(existingByName);
                        target.Id = existingByName.Id;
                    }
                    else
                    {
                        target.CreatedAt = DateTime.UtcNow;
                        await context.ExerciseCatalogItems.AddAsync(target);
                    }
                }
            }
            await context.SaveChangesAsync();

            // Refresh mappings
            var currentEquipments = await context.Equipments.ToListAsync();
            var currentExercises = await context.ExerciseCatalogItems.ToListAsync();

            var eqDict = currentEquipments.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);
            var exDict = currentExercises.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);

            // 6. Define required links
            var expectedRequirements = new List<(string ExerciseName, string EquipmentName)>
            {
                ("Dumbbell Bench Press", "Dumbbells"),
                ("Dumbbell Bench Press", "Bench"),
                ("Barbell Squat", "Barbell"),
                ("Barbell Squat", "Squat Rack"),
                ("Pull-up", "Pull-up Bar"),
                ("Cable Row", "Cable Machine"),
                ("Leg Press", "Leg Press Machine"),
                ("Treadmill Running", "Treadmill"),
                ("Stationary Bike Cycling", "Stationary Bike"),
                ("Lat Pulldown", "Lat Pulldown Machine"),
                ("Smith Machine Incline Bench Press", "Smith Machine"),
                ("Smith Machine Incline Bench Press", "Bench"),
                ("Leg Extension", "Leg Extension Machine"),
                ("Lying Leg Curl", "Leg Curl Machine"),
                ("Pec Deck Fly", "Pec Deck Machine"),
                ("Tricep Pushdown", "Cable Machine"),
                ("Dumbbell Bicep Curl", "Dumbbells"),
                ("Barbell Deadlift", "Barbell"),
                ("Chest Dips", "Dips Bar"),
                ("Rowing Machine Training", "Rowing Machine"),
                ("EZ-Bar Preacher Curl", "EZ Bar"),
                ("EZ-Bar Preacher Curl", "Preacher Curl Bench"),
                ("Standing Calf Raise", "Calf Raise Machine"),
                ("Cable Crossover Chest Fly", "Cable Crossover Machine"),
                ("Machine Crunch", "Abdominal Crunch Machine"),
                ("Assisted Pull-up", "Assisted Pull-up / Dip Machine"),
                ("Assisted Chest Dip", "Assisted Pull-up / Dip Machine"),
                ("Medicine Ball Russian Twist", "Medicine Ball"),
                ("Battle Ropes Waves", "Battle Ropes"),
                ("Hyperextension", "Hyperextension Bench"),
                ("Foam Roller IT Band Roll", "Foam Roller"),
                ("TRX Suspension Row", "TRXs / Suspension Trainer"),
                ("Yoga Mat Plank", "Yoga Mat"),
                ("Dumbbell Incline Bench Press", "Dumbbells"),
                ("Dumbbell Incline Bench Press", "Bench"),
                ("Dumbbell Fly", "Dumbbells"),
                ("Dumbbell Fly", "Bench"),
                ("Barbell Bench Press", "Barbell"),
                ("Barbell Bench Press", "Bench"),
                ("Dumbbell Shoulder Press", "Dumbbells"),
                ("Dumbbell Shoulder Press", "Bench"),
                ("Dumbbell Lateral Raise", "Dumbbells"),
                ("Barbell Overhead Press", "Barbell"),
                ("Face Pull", "Cable Machine"),
                ("Dumbbell Hammer Curl", "Dumbbells"),
                ("Overhead Dumbbell Tricep Extension", "Dumbbells"),
                ("Cable Tricep Overhead Extension", "Cable Machine"),
                ("Barbell Bent-Over Row", "Barbell"),
                ("Dumbbell One-Arm Row", "Dumbbells"),
                ("Dumbbell One-Arm Row", "Bench"),
                ("Bulgarian Split Squat", "Dumbbells"),
                ("Bulgarian Split Squat", "Bench"),
                ("Dumbbell Romanian Deadlift", "Dumbbells"),
                ("Lying Leg Raise", "Yoga Mat"),
                ("Bicycle Crunch", "Yoga Mat"),
                ("Mountain Climbers", "Yoga Mat"),
                ("Elliptical Cross Training", "Elliptical Trainer")
            };

            // Build list of target relationships
            var targetReqList = new List<ExerciseEquipmentRequirement>();
            foreach (var req in expectedRequirements)
            {
                if (exDict.TryGetValue(req.ExerciseName, out var exerciseId) && eqDict.TryGetValue(req.EquipmentName, out var equipmentId))
                {
                    targetReqList.Add(new ExerciseEquipmentRequirement
                    {
                        ExerciseCatalogItemId = exerciseId,
                        EquipmentId = equipmentId
                    });
                }
            }

            // Remove obsolete/orphan requirements
            var currentRequirements = await context.ExerciseEquipmentRequirements.ToListAsync();
            var toDelete = new List<ExerciseEquipmentRequirement>();

            foreach (var req in currentRequirements)
            {
                // If it doesn't match any target relationship, delete it
                var isTarget = targetReqList.Any(t => t.ExerciseCatalogItemId == req.ExerciseCatalogItemId && t.EquipmentId == req.EquipmentId);
                if (!isTarget)
                {
                    toDelete.Add(req);
                }
            }

            if (toDelete.Any())
            {
                context.ExerciseEquipmentRequirements.RemoveRange(toDelete);
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seeder] Removed {toDelete.Count} obsolete requirements.");
            }

            // Add missing requirements
            var toAdd = new List<ExerciseEquipmentRequirement>();
            currentRequirements = await context.ExerciseEquipmentRequirements.ToListAsync(); // refresh

            foreach (var target in targetReqList)
            {
                var exists = currentRequirements.Any(r => r.ExerciseCatalogItemId == target.ExerciseCatalogItemId && r.EquipmentId == target.EquipmentId);
                if (!exists)
                {
                    target.Id = Guid.NewGuid();
                    target.CreatedAt = DateTime.UtcNow;
                    toAdd.Add(target);
                }
            }

            if (toAdd.Any())
            {
                await context.ExerciseEquipmentRequirements.AddRangeAsync(toAdd);
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seeder] Added {toAdd.Count} new requirements.");
            }
        }
    }
}
