using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Chat;
using ArenaDomain.Entities.Health;
using ArenaDomain.Entities.Notifications;
using ArenaDomain.Entities.Nutrition;
using ArenaDomain.Entities.Payments;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Entities.User;
using ArenaDomain.Entities.Workout;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text;

namespace ArenaInfrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext(options)
    {
        // ── Bookings ──────────────────────────────────────────────────────────────
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }

        // ── Chat ──────────────────────────────────────────────────────────────────
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        // ── Health ────────────────────────────────────────────────────────────────
        public DbSet<ProgressLog> ProgressLogs { get; set; }

        // ── Notifications ─────────────────────────────────────────────────────────
        public DbSet<Notification> Notifications { get; set; }

        // ── Nutrition ─────────────────────────────────────────────────────────────
        public DbSet<NutritionPlan> NutritionPlans { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }

        // ── Payments ──────────────────────────────────────────────────────────────
        public DbSet<Payment> Payments { get; set; }

        // ── Subscription ──────────────────────────────────────────────────────────
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }

        // ── User ──────────────────────────────────────────────────────────────────
        public DbSet<MemberProfile> MemberProfiles { get; set; }

        // ── Workout ───────────────────────────────────────────────────────────────
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutDay> WorkoutDays { get; set; }
        public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<Exercise> Exercises { get; set; }


       public DbSet<ArenaDomain.Entities.User.ApplicationUser> ApplicationUsers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
