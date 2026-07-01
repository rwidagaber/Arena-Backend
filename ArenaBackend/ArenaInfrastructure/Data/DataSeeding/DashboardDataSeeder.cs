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
        public static async Task SeedAsync(AppDbContext context, bool forceReseed = false)
        {
            // Guard: only seed if no subscriptions exist yet (skip guard when forced)
            if (!forceReseed && await context.UserSubscriptions.AnyAsync())
                return;

            // Clear existing demo data when reseeding
            if (forceReseed)
            {
                context.Attendances.RemoveRange(context.Attendances);
                context.Bookings.RemoveRange(context.Bookings);
                context.Payments.RemoveRange(context.Payments);
                context.UserSubscriptions.RemoveRange(context.UserSubscriptions);
                await context.SaveChangesAsync();
            }

            var rng = new Random();
            var now = DateTime.UtcNow;
            var today = now.Date;

            // ── Load existing member profiles ─────────────────────────────────────
            var memberProfiles = await context.MemberProfiles.Include(mp => mp.User).ToListAsync();

            if (memberProfiles.Count == 0)
                return;

            // ── Load subscription plans ───────────────────────────────────────────
            var plans = await context
                .SubscriptionPlans.Where(p => p.IsActive && !p.IsDeleted)
                .ToListAsync();

            if (plans.Count == 0)
                return;

            var allPlans = plans.ToArray();
            var highestPlan =
                plans.OrderByDescending(p => p.Price).FirstOrDefault() ?? allPlans.Last();

            var subscriptions = new List<UserSubscription>();
            var payments = new List<Payment>();
            var bookings = new List<Booking>();
            var attendances = new List<Attendance>();

            SubscriptionPlan GetRandomPlan() =>
                rng.Next(10) < 8 ? highestPlan : allPlans[rng.Next(allPlans.Length)];

            // ═══════════════════════════════════════════════════════════════════════
            // SUBSCRIPTIONS — per-member history + up to 2 active subscriptions
            // ═══════════════════════════════════════════════════════════════════════

            var shuffled = memberProfiles.OrderBy(_ => rng.Next()).ToList();

            PaymentMethod RandomPaymentMethod() =>
                (PaymentMethod)new[] { 1, 2, 2, 2, 3, 4 }[rng.Next(6)]; // card-weighted

            void AddSubscription(
                MemberProfile mp,
                SubscriptionPlan plan,
                SubscriptionStatus status,
                DateTime subStart,
                DateTime subEnd
            )
            {
                var sub = new UserSubscription
                {
                    Id = Guid.NewGuid(),
                    MemberProfileId = mp.Id,
                    PlanId = plan.Id,
                    StartDate = subStart,
                    EndDate = subEnd,
                    Status = status,
                    RemainingSessions = status == SubscriptionStatus.Active ? rng.Next(0, 20) : 0,
                    ReminderSent = false,
                    CreatedAt = subStart,
                };
                subscriptions.Add(sub);

                // Payment date within first 3 days of subscription start
                var payDate = subStart.AddDays(rng.Next(0, 3));
                // ±5% price noise so revenue chart has daily variation
                var amount = Math.Round(plan.Price * (decimal)(0.95 + rng.NextDouble() * 0.10), 2);

                payments.Add(
                    new Payment
                    {
                        Id = Guid.NewGuid(),
                        UserId = mp.UserId,
                        UserSubscriptionId = sub.Id,
                        Amount = amount,
                        Currency = "EGP",
                        PaymentMethod = RandomPaymentMethod(),
                        Status = PaymentStatus.Paid,
                        PaymentDate = payDate,
                        CreatedAt = payDate,
                    }
                );
            }

            // How many members get Active subscriptions (make generated data more optimistic)
            int activeCount = Math.Max(1, (int)(shuffled.Count * 0.95));
            int expiringCount = rng.Next(2, Math.Min(5, activeCount)); // expiring-soon subset

            // ── Step 1: Give EVERY member 1–3 past subscriptions (renewal history) ──
            foreach (var mp in shuffled)
            {
                int historyCount = rng.Next(1, 4); // 1, 2, or 3 past subs per member
                // Build a chain ending ~1–30 days before today
                int endDaysAgo = rng.Next(1, 31);

                for (int h = historyCount - 1; h >= 0; h--)
                {
                    int durationDays = rng.Next(28, 91); // 1–3 month duration
                    var subEnd = today.AddDays(-endDaysAgo);
                    var subStart = subEnd.AddDays(-durationDays);

                    // Occasionally cancelled, mostly expired
                    var pastStatus =
                        rng.Next(10) < 2
                            ? SubscriptionStatus.Cancelled
                            : SubscriptionStatus.Expired;

                    AddSubscription(mp, GetRandomPlan(), pastStatus, subStart, subEnd);

                    // Next (older) sub ends where this one started, with a small gap
                    endDaysAgo += durationDays + rng.Next(1, 15);
                }
            }

            // ── Step 2: Assign Active subscriptions ───────────────────────────────
            var activeMembers = shuffled.Take(activeCount).ToList();

            for (int i = 0; i < activeMembers.Count; i++)
            {
                var mp = activeMembers[i];
                bool isExpiringSoon = i < expiringCount;

                int durationDays = rng.Next(28, 91);
                int daysLeft = isExpiringSoon ? rng.Next(1, 7) : rng.Next(10, 46);
                int startDaysAgo = Math.Max(1, durationDays - daysLeft);

                var subStart = today.AddDays(-startDaysAgo);
                var subEnd = subStart.AddDays(durationDays);

                var plan1 = GetRandomPlan();
                AddSubscription(mp, plan1, SubscriptionStatus.Active, subStart, subEnd);

                // All members can have up to 2 plans active (give 80% a second plan)
                if (rng.Next(10) < 8)
                {
                    int dur2 = rng.Next(14, 61);
                    int left2 = rng.Next(5, 30);
                    int ago2 = Math.Max(1, dur2 - left2);
                    var start2 = today.AddDays(-ago2);
                    var end2 = start2.AddDays(dur2);

                    // Pick a different plan for the second subscription
                    var otherPlans = allPlans.Where(p => p.Id != plan1.Id).ToArray();
                    var plan2 =
                        otherPlans.Length > 0
                            ? otherPlans[rng.Next(otherPlans.Length)]
                            : GetRandomPlan();

                    AddSubscription(mp, plan2, SubscriptionStatus.Active, start2, end2);
                }
            }

            // ═══════════════════════════════════════════════════════════════════════
            // ATTENDANCE + BOOKINGS — 90 days, randomized with weekly patterns
            // ═══════════════════════════════════════════════════════════════════════

            // Random base attendance level each run (optimistic)
            int baseAttendance = rng.Next(8, 15);

            // Optimistic growth scenario: always growing
            int scenario = 0;

            // Weights by day-of-week: Mon–Sun (gym is quieter on weekends)
            double[] dayWeights = { 1.1, 1.2, 1.3, 1.1, 0.9, 0.6, 0.4 };

            for (int daysAgo = 89; daysAgo >= 0; daysAgo--)
            {
                var day = today.AddDays(-daysAgo);
                var dowIdx = ((int)day.DayOfWeek + 6) % 7; // 0=Mon…6=Sun
                var progress = (89.0 - daysAgo) / 89.0; // 0→1 over 90 days

                // Growth/decline factor depending on scenario
                double trendFactor = scenario switch
                {
                    0 => 1.0 + 0.4 * progress, // growing +40%
                    1 => 1.3 - 0.4 * progress, // declining
                    2 => 1.0 + 0.15 * Math.Sin(progress * Math.PI * 4), // wave / volatile
                    _ => 1.0, // flat
                };

                // Random daily spike/dip ±30%
                double noise = 0.7 + rng.NextDouble() * 0.6;

                int count = (int)
                    Math.Round(baseAttendance * dayWeights[dowIdx] * trendFactor * noise);
                count = Math.Max(0, Math.Min(count, memberProfiles.Count));

                // No future attendances
                if (day > today)
                    continue;

                var dayMembers = memberProfiles.OrderBy(_ => rng.Next()).Take(count).ToList();

                foreach (var mp in dayMembers)
                {
                    var hour = rng.Next(7, 21); // gym open 07:00–21:00
                    var checkIn = day.AddHours(hour).AddMinutes(rng.Next(0, 60));
                    if (checkIn > now)
                        checkIn = now.AddMinutes(-rng.Next(5, 30));

                    // Mostly Confirmed/Completed; small chance Cancelled (no-show)
                    var bStatus =
                        rng.Next(10) < 2
                            ? BookingStatus.Cancelled
                            : (day < today ? BookingStatus.Completed : BookingStatus.Confirmed);

                    var booking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        MemberProfileId = mp.Id,
                        BookingDate = day,
                        StartTime = TimeSpan.FromHours(hour),
                        Status = bStatus,
                        CreatedAt = day,
                    };
                    bookings.Add(booking);

                    // Only confirmed/completed bookings get an attendance record
                    if (bStatus != BookingStatus.Cancelled)
                    {
                        attendances.Add(
                            new Attendance
                            {
                                Id = Guid.NewGuid(),
                                BookingId = booking.Id,
                                MemberProfileId = mp.Id,
                                CheckInTime = checkIn,
                                CreatedAt = checkIn,
                            }
                        );
                    }
                }
            }

            // ── Persist ────────────────────────────────────────────────────────────
            await context.UserSubscriptions.AddRangeAsync(subscriptions);
            await context.Payments.AddRangeAsync(payments);
            await context.Bookings.AddRangeAsync(bookings);
            await context.Attendances.AddRangeAsync(attendances);
            await context.SaveChangesAsync();
        }
    }
}
