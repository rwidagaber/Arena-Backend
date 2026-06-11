using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArenaDomain.Entities;
using ArenaDomain.Entities.Bookings;
using ArenaDomain.Entities.Payments;
using ArenaDomain.Entities.Subscription;
using ArenaDomain.Enums;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaInfrastructure.Data.DataSeeding
{
    public static class DashboardDataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Guard: only seed if no subscriptions exist yet
            if (await context.UserSubscriptions.AnyAsync())
                return;

            var now = DateTime.UtcNow;
            var today = now.Date;

            // ── Load existing member profiles ──────────────────────────────────
            var memberProfiles = await context.MemberProfiles
                .Include(mp => mp.User)
                .ToListAsync();

            if (memberProfiles.Count == 0)
                return; // nothing to attach to

            // ── Load existing subscription plans (Basic / Premium / Elite) ─────
            var plans = await context.SubscriptionPlans
                .Where(p => p.IsActive && !p.IsDeleted)
                .ToListAsync();

            if (plans.Count == 0)
                return;

            var basicPlan   = plans.FirstOrDefault(p => p.NameEn.Contains("Basic"))   ?? plans[0];
            var premiumPlan = plans.FirstOrDefault(p => p.NameEn.Contains("Premium")) ?? plans[Math.Min(1, plans.Count - 1)];
            var elitePlan   = plans.FirstOrDefault(p => p.NameEn.Contains("Elite"))   ?? plans[Math.Min(2, plans.Count - 1)];

            var subscriptions = new List<UserSubscription>();
            var payments      = new List<Payment>();
            var bookings      = new List<Booking>();
            var attendances   = new List<Attendance>();

            // ── Subscription data layout (up to 8 members, 10 total subscriptions) ─
            // Index 0-1: Active, expire in ~6 days (Expiring Soon)
            // Index 2:   Active, expire in ~5 days (Expiring Soon)
            // Index 3-6: Active, expire next month
            // Index 7:   Expired
            // Extra:     Expired + Cancelled (reuse last two members if < 10 total profiles)

            var subDefs = new[]
            {
                // MemberIndex, Plan, Status, StartOffsetDays, EndOffsetDays, PayThisMonth
                (0, basicPlan,   SubscriptionStatus.Active,     -24,   6,  true),   // expiring soon
                (1, premiumPlan, SubscriptionStatus.Active,     -23,   5,  true),   // expiring soon
                (2, elitePlan,   SubscriptionStatus.Active,     -22,   4,  true),   // expiring soon
                (3, basicPlan,   SubscriptionStatus.Active,     -10,  20,  true),   // healthy
                (4, premiumPlan, SubscriptionStatus.Active,      -5,  25,  true),   // healthy
                (5, elitePlan,   SubscriptionStatus.Active,      -2,  28,  true),   // healthy
                (6, basicPlan,   SubscriptionStatus.Active,      -1,  29,  true),   // healthy
                (Math.Min(7, memberProfiles.Count - 1), premiumPlan, SubscriptionStatus.Expired,  -60, -1,  false),  // expired
                (Math.Min(7, memberProfiles.Count - 1), elitePlan,   SubscriptionStatus.Expired,  -90,-30,  false),  // expired (prev month payment)
                (Math.Min(6, memberProfiles.Count - 1), basicPlan,   SubscriptionStatus.Cancelled,-45,-15,  false),  // cancelled
            };

            foreach (var (memberIdx, plan, status, startOffset, endOffset, payThisMonth) in subDefs)
            {
                var mp = memberProfiles[memberIdx];
                var subStart = now.AddDays(startOffset);
                var subEnd   = now.AddDays(endOffset);

                var sub = new UserSubscription
                {
                    Id               = Guid.NewGuid(),
                    MemberProfileId  = mp.Id,
                    PlanId           = plan.Id,
                    StartDate        = subStart,
                    EndDate          = subEnd,
                    Status           = status,
                    RemainingSessions = 0,
                    ReminderSent     = false,
                    CreatedAt        = subStart
                };
                subscriptions.Add(sub);

                // Payment for every subscription
                DateTime? payDate = payThisMonth
                    ? now.AddDays(-Math.Abs(startOffset) % 10)     // spread in current month
                    : now.AddMonths(-1).AddDays(startOffset + 30); // previous month

                var payment = new Payment
                {
                    Id                  = Guid.NewGuid(),
                    UserId              = mp.UserId,
                    UserSubscriptionId  = sub.Id,
                    Amount              = plan.Price,
                    Currency            = "EGP",
                    PaymentMethod       = PaymentMethod.Card,
                    Status              = PaymentStatus.Paid,
                    PaymentDate         = payDate,
                    CreatedAt           = payDate ?? now
                };
                payments.Add(payment);
            }

            // ── Attendance seed: distribute across Mon-Sun of current week + today ─
            // Monday of current week
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            var weekMonday = today.AddDays(-daysSinceMonday);

            // Attendance counts per day: Mon=3, Tue=4, Wed=5, Thu=3, Fri=2, Sat=1, Sun=0
            //   + make sure TODAY has 4 entries (overrides the day's default)
            var attendanceCounts = new[] { 3, 4, 5, 3, 2, 1, 0 }; // Mon...Sun
            var todayDayIndex = daysSinceMonday; // 0=Mon … 6=Sun
            attendanceCounts[todayDayIndex] = 4; // ensure today always shows 4

            var profileCycle = 0;
            for (int dayIdx = 0; dayIdx < 7; dayIdx++)
            {
                var day   = weekMonday.AddDays(dayIdx);
                var count = attendanceCounts[dayIdx];

                for (int k = 0; k < count; k++)
                {
                    var mp = memberProfiles[profileCycle % memberProfiles.Count];
                    profileCycle++;

                    var checkinHour = 8 + (k * 2); // 08:00, 10:00, 12:00, 14:00…
                    var checkIn = day.AddHours(checkinHour);

                    // Don't create future check-ins (for days that haven't happened yet this week)
                    if (checkIn > now) checkIn = now.AddMinutes(-((dayIdx + 1) * 10));

                    var booking = new Booking
                    {
                        Id              = Guid.NewGuid(),
                        MemberProfileId = mp.Id,
                        BookingDate     = day,
                        StartTime       = TimeSpan.FromHours(checkinHour),
                        Status          = BookingStatus.Confirmed,
                        CreatedAt       = day
                    };
                    bookings.Add(booking);

                    var att = new Attendance
                    {
                        Id              = Guid.NewGuid(),
                        BookingId       = booking.Id,
                        MemberProfileId = mp.Id,
                        CheckInTime     = checkIn,
                        CreatedAt       = checkIn
                    };
                    attendances.Add(att);
                }
            }

            // ── Persist ────────────────────────────────────────────────────────
            await context.UserSubscriptions.AddRangeAsync(subscriptions);
            await context.Payments.AddRangeAsync(payments);
            await context.Bookings.AddRangeAsync(bookings);
            await context.Attendances.AddRangeAsync(attendances);
            await context.SaveChangesAsync();
        }
    }
}
