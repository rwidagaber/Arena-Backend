using ArenaApplication.AI;
using ArenaApplication.Dtos.ChatDtos;
using ArenaApplication.Dtos.WorkoutDtos;
using ArenaApplication.Dtos.WorkoutPlan;
using ArenaApplication.IServices;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.Workout;
using ArenaDomain.Enums;
using ArenaDomain.Interfaces;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArenaInfrastructure.AI
{
    public class WorkoutPlanAIResponse
    {
        public string Name { get; set; } = string.Empty;
        public int DurationWeeks { get; set; }
        public List<WorkoutDayAIResponse> Days { get; set; } = [];
    }

    public class WorkoutDayAIResponse
    {
        public string DayName { get; set; } = string.Empty;
        public List<WorkoutExerciseAIResponse> Exercises { get; set; } = [];
    }

    public class WorkoutExerciseAIResponse
    {
        public string Name { get; set; } = string.Empty;
        public int Sets { get; set; }
        public int Reps { get; set; }
        public string MuscleGroup { get; set; } = string.Empty;
    }

    public class WorkoutAIService : IWorkoutAIService
    {
        private readonly IOpenAIService _openAI;
        private readonly AppDbContext _context;
        private readonly IGenericRepository<MemberProfile, Guid> _memberRepo;
        private readonly IGenericRepository<UserSubscription, Guid> _subscriptionRepo;

        
        public WorkoutAIService(IOpenAIService openAI, AppDbContext context,IGenericRepository<MemberProfile, Guid> memberProfile, IGenericRepository<UserSubscription, Guid> userSubscription)
        {
            _openAI = openAI;
            _context = context;
            _memberRepo = memberProfile;
            _subscriptionRepo = userSubscription;

        }



        public async Task<WorkoutPlanDto> GenerateWorkoutPlanAsync(
      Guid memberProfileId, string userMessage)
        {
            // ✅ Search by Id OR UserId
            var profile = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.Id == memberProfileId
                                       || p.UserId == memberProfileId);

            if (profile == null)
                throw new Exception($"Profile not found: {memberProfileId}");

            // ✅ Log profile data to verify
            Console.WriteLine($"=== PROFILE ===");
            Console.WriteLine($"Name: {profile.FirstName}");
            Console.WriteLine($"Goal: {profile.Goal}");
            Console.WriteLine($"Injuries: {profile.Injuries}");
            Console.WriteLine($"Experience: {profile.FitnessExperience}");
            Console.WriteLine($"Dietary: {profile.DietaryRestrictions}");
            Console.WriteLine($"===============");

            var subscription = _subscriptionRepo.GetAll()
                .FirstOrDefault(s => s.MemberProfileId == profile.Id
                                  && s.Status == SubscriptionStatus.Active);

            var userContext = UserContextBuilder.Build(profile, subscription);

            var knowledge = GymKnowledge.GetExerciseGuide(
                profile.Goal ?? "General Fitness",
                profile.FitnessExperience ?? "Beginner");

            if (!string.IsNullOrEmpty(profile.Injuries))
                knowledge += GymKnowledge.GetInjuryGuide(profile.Injuries);

            //var prompt = PromptBuilder.BuildWorkoutPrompt(
            //    profile, userMessage, userContext + "\n" + knowledge);
            var prompt = PromptLoader.GetWorkoutPrompt(
    userContext: userContext,
    name: profile.FirstName ?? "User",
    goal: profile.Goal ?? "General Fitness",
    injuries: profile.Injuries ?? "None",
    healthConditions: profile.HealthConditions ?? "None",
    experience: profile.FitnessExperience ?? "Beginner",
    equipment: profile.Equipment ?? "Full Gym",
    userMessage: userMessage);



            var jsonResponse = await _openAI.GetCompletionAsync(
                prompt, new List<ChatMessageDto>(), "Generate the plan");

            Console.WriteLine("=== WORKOUT RAW ===");
            Console.WriteLine(jsonResponse);
            Console.WriteLine("===================");

            var cleanJson = AIHelper.CleanJson(jsonResponse);

            Console.WriteLine("=== WORKOUT CLEAN ===");
            Console.WriteLine(cleanJson);
            Console.WriteLine("=====================");

            var planData = JsonSerializer.Deserialize<WorkoutPlanAIResponse>(
                cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (planData == null)
                throw new Exception("AI returned invalid workout plan JSON");

            var plan = new WorkoutPlan
            {
                MemberProfileId = profile.Id,
                Name = planData.Name,
                DurationWeeks = planData.DurationWeeks,
                IsActive = true
            };

            _context.WorkoutPlans.Add(plan);
            await _context.SaveChangesAsync();

            var dayDtos = new List<WorkoutDayDto>();

            foreach (var day in planData.Days ?? [])
            {
                var workoutDay = new WorkoutDay
                {
                    WorkoutPlanId = plan.Id,
                    DayName = day.DayName
                };
                _context.WorkoutDays.Add(workoutDay);
                await _context.SaveChangesAsync();

                var exerciseDtos = new List<WorkoutExerciseDto>();

                foreach (var ex in day.Exercises ?? [])
                {
                    var existingExercise = await _context.Exercises
                        .FirstOrDefaultAsync(e => e.Name == ex.Name
                                               && e.MemberProfileId == profile.Id);

                    if (existingExercise == null)
                    {
                        existingExercise = new Exercise
                        {
                            Id = Guid.NewGuid(),
                            Name = ex.Name,
                            MuscleGroup = ex.MuscleGroup,
                            Description = ex.Name,
                            Equipment = "None",
                            MemberProfileId = profile.Id
                        };
                        _context.Exercises.Add(existingExercise);
                        await _context.SaveChangesAsync();
                    }

                    _context.WorkoutExercises.Add(new WorkoutExercise
                    {
                        WorkoutDayId = workoutDay.Id,
                        ExerciseId = existingExercise.Id,
                        ExrciseName = ex.Name,
                        Sets = ex.Sets,
                        Reps = ex.Reps
                    });

                    exerciseDtos.Add(new WorkoutExerciseDto
                    {
                        Name = ex.Name,
                        Sets = ex.Sets,
                        Reps = ex.Reps
                    });
                }

                await _context.SaveChangesAsync();

                dayDtos.Add(new WorkoutDayDto
                {
                    Id = workoutDay.Id,
                    WorkoutPlanId = plan.Id,
                    DayName = day.DayName,
                    Exercises = exerciseDtos
                });
            }

            return new WorkoutPlanDto
            {
                Id = plan.Id,
                MemberProfileId = plan.MemberProfileId,
                AssignedTrainerId = plan.AssignedTrainerId,
                Name = plan.Name,
                DurationWeeks = plan.DurationWeeks,
                IsActive = plan.IsActive,
                Days = dayDtos

            };
        }
    }
}