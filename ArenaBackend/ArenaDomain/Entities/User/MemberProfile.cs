using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Chat;
using ArenaDomain.Entities.Health;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.User;
using ArenaDomain.Entities.Workout;
using ArenaDomain.Enums;
using ArenaDomain.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ArenaDomain.Entities
{
    public class MemberProfile : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;

        public DateTime DateOfBirth { get; set; }

        public decimal? Weight { get; set; }

        public decimal? Height { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MuscleMass { get; set; }

        public decimal? BMI { get; set; }

        public Gender Gender { get; set; }

        public string? ProfileImageUrl { get; set; }

        public string? FirstName { get; set; }


        public string? Goal { get; set; }
        // "WeightLoss" | "MuscleGain" | "Endurance" | "GeneralFitness"

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetWeight { get; set; }

        public string? ActivityLevel { get; set; }
        // "Sedentary" | "Light" | "Moderate" | "Active" | "VeryActive"

        public string? HealthConditions { get; set; }
        // "Diabetes, Knee Injury, Hypertension"

        public string? Injuries { get; set; }
        // "Lower back pain, Left knee"

        public string? FitnessExperience { get; set; }
        // "Beginner" | "Intermediate" | "Advanced"

        public string? DietaryRestrictions { get; set; }
        // "Vegetarian, Lactose Intolerant"

        public string? Equipment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetCalories { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetProtein { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetCarbs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetFat { get; set; }

        public string? CurrentPlanFramework { get; set; }

        public string? HealthProfileJson { get; set; } // For Health Intelligence Layer

        // Navigation Properties
        public virtual ICollection<UserSubscription> Subscriptions { get; set; } = [];
        public virtual ICollection<Notification> Notifications { get; set; } = [];

        public virtual ICollection<Booking> Bookings { get; set; } = [];

        public virtual ICollection<Attendance> Attendances { get; set; } = [];

        public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = [];

        public virtual ICollection<NutritionPlan> NutritionPlans { get; set; } = [];

        public virtual ICollection<MealLog> MealLogs { get; set; } = [];

        public virtual ICollection<ProgressLog> ProgressLogs { get; set; } = [];

        public virtual ICollection<ChatConversation> ChatConversations { get; set; } = [];

        public virtual ICollection<MemberHealthVector> HealthVectors { get; set; } = [];



    }
}
