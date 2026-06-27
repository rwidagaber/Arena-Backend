# Localization for Equipments, Exercises & ExerciseCatalogItems

## Background

The app already has a proven localization pattern for `SubscriptionPlan` — bilingual columns (`NameEn`/`NameAr`, `DescriptionEn`/`DescriptionAr`). We will apply that exact same pattern to the three affected tables and populate the Arabic columns using the existing Gemini AI service (`IGeminiCompletionService`).

### Affected tables

| Table | New Columns |
|---|---|
| `Equipments` | `NameAr` |
| `Exercises` | `NameAr`, `DescriptionAr`, `MuscleGroupAr`, `EquipmentAr` |
| `ExerciseCatalogItems` | `NameAr`, `DescriptionAr`, `MuscleGroupAr` |

> [!NOTE]
> `Exercises` is a **member-owned** entity (linked to `MemberProfileId`). No seeder data exists there — members create exercises through the AI Workout service. Localized fields will be added but seeding with AI translations is **not applicable** for that table. Only `Equipments` and `ExerciseCatalogItems` have seeded data to translate.

---

## Open Questions

> [!IMPORTANT]
> **Frontend (Angular) scope**: The Angular frontend only consumes `WorkoutPlans` via `WorkoutService`, which returns `Exercise` records inside `WorkoutExerciseDtos`. Should the frontend **display the Arabic name** when the user's language is Arabic, or should English always be shown in the app? This would determine whether `NameAr` etc. need to be exposed through the API DTOs. **Assumption for now: expose them via API DTOs so the frontend can choose.** Please confirm.

> [!IMPORTANT]
> **Arabic hardcoded comments in `workout.ts`**: The Angular model file `workout.ts` contains Arabic-language *code comments* (e.g. `// 1. الأصغر:`). These are cosmetic only and do not affect runtime behavior. Should they be replaced with English comments? **Assumption: yes, for code consistency.** Please confirm.

---

## Proposed Changes

### Phase 1 — Domain Entities (Schema)

---

#### [MODIFY] [Equipment.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaDomain/Entities/Gym/Equipment.cs)
Add `NameAr` nullable column (Arabic name of the equipment).

#### [MODIFY] [Exercise.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaDomain/Entities/Workout/Exercise.cs)
Add `NameAr`, `DescriptionAr`, `MuscleGroupAr`, `EquipmentAr` nullable columns.

#### [MODIFY] [ExerciseCatalogItem.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaDomain/Entities/Workout/ExerciseCatalogItem.cs)
Add `NameAr`, `DescriptionAr`, `MuscleGroupAr` nullable columns.

---

### Phase 2 — EF Core Configurations

---

#### [MODIFY] [ExerciseConfiguration.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Data/Configurations/ExerciseConfiguration.cs)
Add `HasMaxLength` constraints for the new Arabic columns (all `nvarchar` nullable).

No `EquipmentConfiguration` exists yet; the `Equipment` entity relies on convention. We will add a new configuration file.

#### [NEW] `EquipmentConfiguration.cs`
Path: `ArenaInfrastructure/Data/Configurations/EquipmentConfiguration.cs`  
Configure `NameAr` as nullable, `MaxLength(200)`. Table name `Equipments`.

#### [NEW] `ExerciseCatalogItemConfiguration.cs`
Path: `ArenaInfrastructure/Data/Configurations/ExerciseCatalogItemConfiguration.cs`  
Configure Arabic columns as nullable. Table name `ExerciseCatalogItems`.

---

### Phase 3 — Database Migration

---

#### [NEW] EF Migration `AddLocalizationToEquipmentsAndExercises`
```
dotnet ef migrations add AddLocalizationToEquipmentsAndExercises --project ArenaInfrastructure --startup-project ArenaAPI
dotnet ef database update --project ArenaInfrastructure --startup-project ArenaAPI
```
The migration will:
- `ALTER TABLE Equipments ADD NameAr nvarchar(200) NULL`
- `ALTER TABLE Exercises ADD NameAr, DescriptionAr, MuscleGroupAr, EquipmentAr (all nvarchar NULL)`
- `ALTER TABLE ExerciseCatalogItems ADD NameAr, DescriptionAr, MuscleGroupAr (all nvarchar NULL)`

---

### Phase 4 — Application DTOs

---

#### [MODIFY] [EquipmentDto.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaApplication/Dtos/Gym/EquipmentDto.cs)
Add `public string? NameAr { get; set; }`.

#### [MODIFY] [ExerciseCatalogItemDto.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaApplication/Dtos/Workout/ExerciseCatalogItemDto.cs)
Add `NameAr`, `DescriptionAr`, `MuscleGroupAr` nullable string properties.

#### [MODIFY] [ExerciseDto.cs (WorkoutDtos)](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaApplication/Dtos/WorkoutDtos/ExerciseDto.cs)
Add `NameAr`, `DescriptionAr`, `MuscleGroupAr`, `EquipmentAr` nullable string properties.

---

### Phase 5 — Services (Read/Write)

---

#### [MODIFY] [EquipmentService.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaApplication/Services/Gym/EquipmentService.cs)
- Map `NameAr` in all `EquipmentDto` projections (Get, GetAll, Create, Update).
- Pass `NameAr` through Create/Update.

#### [MODIFY] [ExerciseCatalogService.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaApplication/Services/Gym/ExerciseCatalogService.cs)
- Map `NameAr`, `DescriptionAr`, `MuscleGroupAr` in all projections.
- Pass through Create/Update.

Workout plan service (`WorkoutPlanService`, `WorkoutAIService`) — the `Exercise` entity is created by the AI service. We will update `WorkoutAIService` to populate `NameAr`, `DescriptionAr`, `MuscleGroupAr`, `EquipmentAr` by including them in the AI generation prompt so new exercises are bilingual from creation.

---

### Phase 6 — MVC Admin Views (CRUD forms)

---

#### [MODIFY] [Equipments/Create.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/Equipments/Create.cshtml)
Add a new form field **"Arabic Name (NameAr)"** below the English Name field, with RTL direction styling.

#### [MODIFY] [Equipments/Edit.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/Equipments/Edit.cshtml)
Same as Create — add `NameAr` field.

#### [MODIFY] [ExerciseCatalog/Create.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/ExerciseCatalog/Create.cshtml)
Add `NameAr`, `DescriptionAr`, `MuscleGroupAr` fields with RTL hint.

#### [MODIFY] [ExerciseCatalog/Edit.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/ExerciseCatalog/Edit.cshtml)
Same as Create.

#### [MODIFY] [Equipments/_EquipmentResults.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/Equipments/_EquipmentResults.cshtml)
Add a small Arabic name column (or show it as secondary text under the English name).

#### [MODIFY] [ExerciseCatalog/_ExerciseResults.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/ExerciseCatalog/_ExerciseResults.cshtml)
Same pattern.

---

### Phase 7 — Localization Resource Files

---

#### [MODIFY] [en-US.json](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Resources/en-US.json)
Add new localization keys for all Equipment & Exercise CRUD operations (mirrors the existing SubscriptionPlan and WorkingHours pattern):
```json
"Equipments": "Equipments",
"ManageGymEquipment": "Manage gym equipment ...",
"EquipmentCreatedSuccessfully": "Equipment created successfully.",
"EquipmentUpdatedSuccessfully": "Equipment updated successfully.",
"EquipmentDeletedSuccessfully": "Equipment deleted successfully.",
"EquipmentNotFound": "Equipment not found.",
"AnErrorOccurredRetrievingEquipments": "...",
...
"EquipmentNameAr": "Arabic Name",
"ExerciseCatalog": "Exercise Catalog",
...
"ExerciseNameAr": "Arabic Exercise Name",
"ExerciseDescriptionAr": "Arabic Description",
"ExerciseMuscleGroupAr": "Arabic Muscle Group"
```

#### [MODIFY] [ar-EG.json](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Resources/ar-EG.json)
Add corresponding Arabic translations for all new keys above.

---

### Phase 8 — AI-Powered Arabic Translation Seeder

---

#### [MODIFY] [EquipmentAndExerciseSeeder.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Data/DataSeeding/EquipmentAndExerciseSeeder.cs)
Refactor to:
1. **Populate `NameAr` in seeder data** using hardcoded AI-generated Arabic translations (run once; safe for CI/CD environments without API key access at migration time).
2. Add a second optional method `TranslateExistingRecordsAsync(AppDbContext context, IGeminiCompletionService gemini)` that can be called if the database already has existing rows with null `NameAr`.

The Arabic translations to be included in seeder data:

| Equipment | `NameAr` |
|---|---|
| Dumbbells | دمبلز |
| Barbell | بار بل |
| Squat Rack | رف القرفصاء |
| Bench | المقعد |
| Pull-up Bar | عارضة العقلة |
| Cable Machine | آلة الكابل |
| Leg Press Machine | آلة ضغط الأرجل |
| Treadmill | جهاز الجري |
| Stationary Bike | الدراجة الثابتة |
| Kettlebell | كيتل بيل |

| Exercise | `NameAr` | `DescriptionAr` | `MuscleGroupAr` |
|---|---|---|---|
| Dumbbell Bench Press | ضغط الدمبل على المقعد | اضغط الدمبلات وأنت مستلقٍ على المقعد. | الصدر |
| Barbell Squat | قرفصاء بالبار بل | القرفصاء مع بار بل على الكتفين. | الأرجل |
| Pull-up | عقلة | ارفع جسمك للأعلى حتى العارضة. | الظهر |
| Cable Row | سحب الكابل | اسحب الكابل نحو الجذع. | الظهر |
| Leg Press | ضغط الأرجل | ادفع الثقل بعيداً بالأرجل. | الأرجل |
| Treadmill Running | الجري على جهاز الجري | الجري على جهاز الجري. | كارديو |
| Stationary Bike Cycling | ركوب الدراجة الثابتة | ركوب الدراجة الثابتة. | كارديو |
| Kettlebell Swing | تأرجح الكيتل بيل | تأرجح الكيتل بيل بين الساقين وحتى مستوى الصدر. | الجسم كله |
| Push-up | تمرين الضغط | ارفع جسمك من الأرض. | الصدر |
| Bodyweight Squat | قرفصاء بدون أوزان | القرفصاء بدون أوزان إضافية. | الأرجل |

---

### Phase 9 — Frontend (Angular) Updates

---

#### [MODIFY] [workout.ts (models)](file:///d:/Learn/ITI/Final%20Project/Arena-Frontend/ArenaFrontend/src/app/core/models/workout.ts)
- Add `nameAr?`, `descriptionAr?`, `muscleGroupAr?`, `equipmentAr?` to `ExerciseDto`.
- Replace Arabic code comments with English comments.

> [!NOTE]
> No Angular components currently display an exercise catalog or equipment list to end users — the frontend only shows workout plans assigned by AI. Therefore, no component-level changes are needed beyond updating the model interface to accept the new fields when they arrive from the API. If a future component needs to switch by locale, it can use `nameAr` vs `name` based on a language check.

---

### Phase 10 — Migrate Past Arabic Exercises Data

---

#### [MODIFY] [EquipmentAndExerciseSeeder.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Data/DataSeeding/EquipmentAndExerciseSeeder.cs)
Add a new method `TranslateExistingRecordsAsync(AppDbContext context, IGeminiCompletionService gemini)`.
- It will fetch all `Exercises` where `NameAr` is null.
- Since past data might be in Arabic (because the AI responded in Arabic based on user language), it will prompt Gemini to take the existing `Name`, `Description`, `MuscleGroup`, `Equipment` and translate/sort them into explicit English and Arabic properties.
- Update the records and call `SaveChangesAsync`.

**Verification**:
Run the seeder method from a test endpoint or controller to verify it translates existing Arabic data and populates `NameAr`.

---

## Verification Plan

### Automated Tests
- `dotnet build` — confirm zero compilation errors after entity/DTO/service changes.
- `dotnet ef migrations list` — confirm new migration appears cleanly.

### Manual Verification
1. Run `dotnet ef database update` and confirm new columns appear in SSMS/Azure Data Studio.
2. Start `ArenaAPI` and navigate to Admin MVC → **Equipments** → Create/Edit: confirm `NameAr` field is visible, can be saved, and shows up in the list.
3. Navigate to Admin MVC → **Exercise Catalog** → Create/Edit: confirm `NameAr`, `DescriptionAr`, `MuscleGroupAr` fields work.
4. Confirm existing seeded rows now have Arabic text populated (via data seeder update or one-time DB script).
5. Verify the Angular app still loads workout plans without errors (backward-compatible since new DTO fields are nullable).
