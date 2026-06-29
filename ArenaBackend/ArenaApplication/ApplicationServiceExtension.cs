using ArenaApplication.IServices;
using ArenaApplication.Services;
using ArenaApplication.Services.Gym;
using ArenaApplication.Services.SubscriptionPlan;
using ArenaApplication.Services.UserSubscription;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaApplication
{
    public static class ApplicationServiceExtension
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
            services.AddScoped<IWorkoutPlanService, WorkoutPlanService>();
            services.AddScoped<INutritionPlanService, NutritionPlanService>();
            services.AddScoped<IWorkingHoursService, WorkingHoursService>();
            services.AddScoped<IMealLogService, MealLogService>();
            services.AddScoped<INutritionPlanActivationService, NutritionPlanActivationService>();

            services.AddScoped<IAttendanceSuggestionService, AttendanceSuggestionService>();
            services.AddScoped<IBookingValidationService, BookingValidationService>();

            services.AddScoped<IGymSettingsService, GymSettingsService>();
            services.AddScoped<INoShowPenaltyService, NoShowPenaltyService>();
            services.AddScoped<IEquipmentService, EquipmentService>();
            services.AddScoped<IEquipmentCategoryService, EquipmentCategoryService>();
            services.AddScoped<IExerciseCatalogService, ExerciseCatalogService>();

            return services;
        }
    }
}
