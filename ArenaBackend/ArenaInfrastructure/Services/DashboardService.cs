using ArenaApplication.Dtos.Dashboard;
using ArenaApplication.Dtos.Dashboard.Analytics;
using ArenaApplication.IServices;
using ArenaDomain.Enums;
using ArenaInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace ArenaInfrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IAnalyticsCacheVersionService _analyticsCacheVersionService;

        public DashboardService(
            AppDbContext context,
            IMemoryCache memoryCache,
            IAnalyticsCacheVersionService analyticsCacheVersionService)
        {
            _context = context;
            _memoryCache = memoryCache;
            _analyticsCacheVersionService = analyticsCacheVersionService;
        }

        public async Task<AdminDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            // ── Cache gate: expensive path runs at most once every 5 minutes ──
            var bucket   = (DateTime.UtcNow.Minute / 5) * 5;
            var cacheKey = $"admin-dashboard|{DateTime.UtcNow:yyyy-MM-dd-HH}-{bucket:D2}";

            if (_memoryCache.TryGetValue(cacheKey, out AdminDashboardDto? cached) && cached is not null)
                return cached;

            var now = DateTime.UtcNow;
            var today = now.Date;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var sevenDaysFromNow = now.AddDays(7);

            // ── Week boundaries (Monday – Sunday) ──────────────────────────────
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            var weekStart = today.AddDays(-daysSinceMonday);
            var weekEnd = weekStart.AddDays(7);

            var dto = new AdminDashboardDto();

            // ── KPI: Total Members ─────────────────────────────────────────────
            dto.TotalMembers = await _context.Users
                .CountAsync(u => !u.IsDeleted, cancellationToken);

            var membersWithActiveSubs = await _context.UserSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted)
                .Select(s => s.MemberProfileId)
                .Distinct()
                .CountAsync(cancellationToken);

            dto.MembersWithoutActiveSubscriptions = dto.TotalMembers - membersWithActiveSubs;

            // ── KPI: Active Subscriptions ──────────────────────────────────────
            dto.ActiveSubscriptions = await _context.UserSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted, cancellationToken);

            // ── KPI: Expiring Subscriptions (next 7 days) ──────────────────────
            dto.ExpiringSubscriptions = await _context.UserSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active
                                 && s.EndDate > now
                                 && s.EndDate <= sevenDaysFromNow
                                 && !s.IsDeleted, cancellationToken);

            // ── KPI: Today's Attendance ────────────────────────────────────────
            var tomorrow = today.AddDays(1);
            dto.TodayAttendance = await _context.Attendances
                .CountAsync(a => a.CheckInTime != null
                                 && a.CheckInTime >= today
                                 && a.CheckInTime < tomorrow, cancellationToken);

            // ── KPI: Monthly Revenue ───────────────────────────────────────────
            dto.MonthlyRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid
                            && p.PaymentDate != null
                            && p.PaymentDate.Value >= currentMonthStart
                            && p.PaymentDate.Value < currentMonthStart.AddMonths(1))
                .Select(p => (decimal?)p.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            // ── KPI: Plans ─────────────────────────────────────────────────────
            var plans = await _context.SubscriptionPlans
                .Where(p => !p.IsDeleted)
                .ToListAsync(cancellationToken);
            dto.TotalPlans = plans.Count;
            dto.ActivePlans = plans.Count(p => p.IsActive);

            // ── Growth: Members (current month vs previous month) ──────────────
            var currentMonthMembers = await _context.Users
                .CountAsync(u => !u.IsDeleted && u.CreatedAt >= currentMonthStart, cancellationToken);

            var previousMonthMembers = await _context.Users
                .CountAsync(u => !u.IsDeleted
                                 && u.CreatedAt >= previousMonthStart
                                 && u.CreatedAt < currentMonthStart, cancellationToken);

            dto.MemberGrowthPercent = CalculateGrowthPercent(currentMonthMembers, previousMonthMembers);

            // ── Growth: Subscriptions (current vs previous month) ──────────────
            var currentMonthSubs = await _context.UserSubscriptions
                .CountAsync(s => !s.IsDeleted
                                 && s.CreatedAt >= currentMonthStart, cancellationToken);

            var previousMonthSubs = await _context.UserSubscriptions
                .CountAsync(s => !s.IsDeleted
                                 && s.CreatedAt >= previousMonthStart
                                 && s.CreatedAt < currentMonthStart, cancellationToken);

            dto.SubscriptionGrowthPercent = CalculateGrowthPercent(currentMonthSubs, previousMonthSubs);

            // ── Growth: Revenue (current vs previous month) ────────────────────
            var previousMonthRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid
                            && p.PaymentDate != null
                            && p.PaymentDate.Value >= previousMonthStart
                            && p.PaymentDate.Value < currentMonthStart)
                .Select(p => (decimal?)p.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            dto.RevenueGrowthPercent = previousMonthRevenue > 0
                ? Math.Round((dto.MonthlyRevenue - previousMonthRevenue) / previousMonthRevenue * 100, 1)
                : (dto.MonthlyRevenue > 0 ? 100m : 0m);

            // ── Last 7 Days Attendance ────────────────────────────────────
            var last7DaysStart = today.AddDays(-6);
            var last7DaysEnd = today.AddDays(1); // To include today up to 23:59:59

            var weeklyCheckIns = await _context.Attendances
                .Where(a => a.CheckInTime != null
                            && a.CheckInTime >= last7DaysStart
                            && a.CheckInTime < last7DaysEnd)
                .Select(a => a.CheckInTime!.Value)
                .ToListAsync(cancellationToken);

            var weeklyData = weeklyCheckIns
                .GroupBy(t => t.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList();

            var last7DaysOrder = Enumerable.Range(0, 7)
                .Select(i => last7DaysStart.AddDays(i).Date)
                .ToList();

            dto.WeeklyAttendance = last7DaysOrder.Select(date => new DailyAttendanceDto
            {
                DayName = date.ToString("ddd", CultureInfo.InvariantCulture),
                Date = date,
                Count = weeklyData.FirstOrDefault(w => w.Date == date)?.Count ?? 0
            }).ToList();

            // ── Recent Check-ins (last 5) ──────────────────────────────────────
            var recentAttendances = await _context.Attendances
                .Where(a => a.CheckInTime != null)
                .OrderByDescending(a => a.CheckInTime)
                .Take(5)
                .Include(a => a.MemberProfile)
                    .ThenInclude(mp => mp.User)
                .Include(a => a.MemberProfile)
                    .ThenInclude(mp => mp.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active))
                        .ThenInclude(s => s.Plan)
                .ToListAsync(cancellationToken);

            dto.RecentCheckIns = recentAttendances.Select(a =>
            {
                var user = a.MemberProfile?.User;
                var firstName = user?.FirstName ?? "";
                var lastName = user?.LastName ?? "";
                var fullName = $"{firstName} {lastName}".Trim();

                var activeSub = a.MemberProfile?.Subscriptions
                    .FirstOrDefault(s => s.Status == SubscriptionStatus.Active);

                var planName = activeSub?.Plan?.NameEn ?? "No Active Plan";

                return new RecentCheckInDto
                {
                    MemberName = string.IsNullOrEmpty(fullName) ? "Unknown Member" : fullName,
                    Initials = GetInitials(firstName, lastName),
                    PlanName = planName,
                    CheckInTime = a.CheckInTime!.Value,
                    AvatarColor = GenerateAvatarColor(fullName)
                };
            }).ToList();

            // ── Store in cache and return ──────────────────────────────────
            _memoryCache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
            return dto;
        }

        public async Task<AnalyticsEnvelopeDto<AdminAnalyticsV2Dto>> GetAnalyticsV2Async(
            AnalyticsQueryWindowDto query,
            CancellationToken cancellationToken = default)
        {
            var window = BuildWindow(query);
            var cacheKey = BuildCacheKey("overview", window);

            if (_memoryCache.TryGetValue(cacheKey, out AnalyticsEnvelopeDto<AdminAnalyticsV2Dto>? cached)
                && cached is not null)
            {
                return cached;
            }

            var now = DateTime.UtcNow;
            var windowDays = Math.Max(1, (int)Math.Ceiling((window.EndUtc - window.StartUtc).TotalDays));

            var totalMembers = await _context.Users
                .CountAsync(u => !u.IsDeleted, cancellationToken);

            var activeSubscriptions = await _context.UserSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted, cancellationToken);

            var expiringSubscriptions = await _context.UserSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active
                                 && s.EndDate > now
                                 && s.EndDate <= now.AddDays(7)
                                 && !s.IsDeleted, cancellationToken);

            var todayBounds = GetLocalDayUtcBounds(now, window.TimezoneInfo);
            var attendanceToday = await _context.Attendances
                .CountAsync(a => a.CheckInTime != null
                                 && a.CheckInTime >= todayBounds.StartUtc
                                 && a.CheckInTime < todayBounds.EndUtc,
                    cancellationToken);

            var revenueInWindow = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid
                            && p.PaymentDate != null
                            && p.PaymentDate.Value >= window.StartUtc
                            && p.PaymentDate.Value < window.EndUtc)
                .Select(p => (decimal?)p.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            var previousStart = window.StartUtc.AddDays(-windowDays);
            var previousEnd = window.StartUtc;

            var previousRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid
                            && p.PaymentDate != null
                            && p.PaymentDate.Value >= previousStart
                            && p.PaymentDate.Value < previousEnd)
                .Select(p => (decimal?)p.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            var revenueGrowthPercent = previousRevenue > 0
                ? Math.Round((revenueInWindow - previousRevenue) / previousRevenue * 100, 1)
                : (revenueInWindow > 0 ? 100m : 0m);

            var dailyRevenue = await BuildRevenueSeriesAsync(window, cancellationToken);
            var dailyAttendance = await BuildAttendanceSeriesAsync(window, cancellationToken);

            var bookingsInWindow = await _context.Bookings
                .CountAsync(b => !b.IsDeleted
                                 && b.BookingDate >= window.StartUtc
                                 && b.BookingDate < window.EndUtc,
                    cancellationToken);

            var checkInsInWindow = dailyAttendance.Sum(x => (int)x.Value);

            var completedSessionsInWindow = await _context.Bookings
                .CountAsync(b => !b.IsDeleted
                                 && b.BookingDate >= window.StartUtc
                                 && b.BookingDate < window.EndUtc
                                 && b.Status == BookingStatus.Completed,
                    cancellationToken);

            var response = new AnalyticsEnvelopeDto<AdminAnalyticsV2Dto>
            {
                Meta = BuildMeta(window, now),
                Data = new AdminAnalyticsV2Dto
                {
                    Executive = new ExecutiveKpisDto
                    {
                        TotalMembers = totalMembers,
                        ActiveSubscriptions = activeSubscriptions,
                        ExpiringSubscriptionsNext7Days = expiringSubscriptions,
                        AttendanceToday = attendanceToday
                    },
                    Financial = new FinancialAnalyticsDto
                    {
                        RevenueInWindow = revenueInWindow,
                        RevenueGrowthPercent = revenueGrowthPercent,
                        DailyRevenue = dailyRevenue
                    },
                    Operational = new OperationalAnalyticsDto
                    {
                        DailyAttendance = dailyAttendance,
                        BookingsInWindow = bookingsInWindow,
                        CheckInsInWindow = checkInsInWindow,
                        CompletedSessionsInWindow = completedSessionsInWindow
                    }
                }
            };

            _memoryCache.Set(cacheKey, response, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            });

            return response;
        }

        public async Task<AnalyticsEnvelopeDto<List<DailyMetricPointDto>>> GetRevenueDrilldownAsync(
            AnalyticsQueryWindowDto query,
            CancellationToken cancellationToken = default)
        {
            var window = BuildWindow(query);
            var cacheKey = BuildCacheKey("drilldown:revenue", window);

            if (_memoryCache.TryGetValue(cacheKey, out AnalyticsEnvelopeDto<List<DailyMetricPointDto>>? cached)
                && cached is not null)
            {
                return cached;
            }

            var now = DateTime.UtcNow;
            var data = await BuildRevenueSeriesAsync(window, cancellationToken);

            var response = new AnalyticsEnvelopeDto<List<DailyMetricPointDto>>
            {
                Meta = BuildMeta(window, now),
                Data = data
            };

            _memoryCache.Set(cacheKey, response, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });

            return response;
        }

        public async Task<AnalyticsEnvelopeDto<List<DailyMetricPointDto>>> GetAttendanceDrilldownAsync(
            AnalyticsQueryWindowDto query,
            CancellationToken cancellationToken = default)
        {
            var window = BuildWindow(query);
            var cacheKey = BuildCacheKey("drilldown:attendance", window);

            if (_memoryCache.TryGetValue(cacheKey, out AnalyticsEnvelopeDto<List<DailyMetricPointDto>>? cached)
                && cached is not null)
            {
                return cached;
            }

            var now = DateTime.UtcNow;
            var data = await BuildAttendanceSeriesAsync(window, cancellationToken);

            var response = new AnalyticsEnvelopeDto<List<DailyMetricPointDto>>
            {
                Meta = BuildMeta(window, now),
                Data = data
            };

            _memoryCache.Set(cacheKey, response, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });

            return response;
        }

        private AnalyticsWindowContext BuildWindow(AnalyticsQueryWindowDto query)
        {
            var now = DateTime.UtcNow;
            var endUtc = query.EndDateUtc?.ToUniversalTime() ?? now;
            var startUtc = query.StartDateUtc?.ToUniversalTime() ?? endUtc.AddDays(-30);
            var flags = new List<string>();

            if (startUtc > endUtc)
            {
                (startUtc, endUtc) = (endUtc, startUtc);
                flags.Add("window_swapped");
            }

            if (startUtc == endUtc)
            {
                startUtc = startUtc.AddDays(-1);
                flags.Add("window_zero_length_adjusted");
            }

            if ((endUtc - startUtc).TotalDays > 366)
            {
                startUtc = endUtc.AddDays(-366);
                flags.Add("window_clamped_366_days");
            }

            var requestedTimezone = string.IsNullOrWhiteSpace(query.Timezone) ? "UTC" : query.Timezone.Trim();
            TimeZoneInfo timezoneInfo;

            try
            {
                timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(requestedTimezone);
            }
            catch
            {
                timezoneInfo = TimeZoneInfo.Utc;
                flags.Add("timezone_fallback_utc");
            }

            return new AnalyticsWindowContext(startUtc, endUtc, timezoneInfo, requestedTimezone, flags);
        }

        private string BuildCacheKey(string scope, AnalyticsWindowContext window)
        {
            var version = _analyticsCacheVersionService.GetVersion();
            return string.Join('|',
                "analytics-v2",
                scope,
                $"v={version}",
                $"start={window.StartUtc:O}",
                $"end={window.EndUtc:O}",
                $"tz={window.TimezoneInfo.Id}");
        }

        private AnalyticsMetaDto BuildMeta(AnalyticsWindowContext window, DateTime generatedAtUtc)
        {
            return new AnalyticsMetaDto
            {
                GeneratedAtUtc = generatedAtUtc,
                DataAsOfUtc = generatedAtUtc,
                StartDateUtc = window.StartUtc,
                EndDateUtc = window.EndUtc,
                Timezone = window.TimezoneInfo.Id,
                DataQualityFlags = [.. window.Flags]
            };
        }

        private (DateTime StartUtc, DateTime EndUtc) GetLocalDayUtcBounds(DateTime utcDateTime, TimeZoneInfo timezoneInfo)
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timezoneInfo);
            var localDayStart = localNow.Date;
            var localDayEnd = localDayStart.AddDays(1);

            return (
                TimeZoneInfo.ConvertTimeToUtc(localDayStart, timezoneInfo),
                TimeZoneInfo.ConvertTimeToUtc(localDayEnd, timezoneInfo)
            );
        }

        private async Task<List<DailyMetricPointDto>> BuildRevenueSeriesAsync(
            AnalyticsWindowContext window,
            CancellationToken cancellationToken)
        {
            var raw = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid
                            && p.PaymentDate != null
                            && p.PaymentDate.Value >= window.StartUtc
                            && p.PaymentDate.Value < window.EndUtc)
                .Select(p => new { Date = p.PaymentDate!.Value, p.Amount })
                .ToListAsync(cancellationToken);

            var grouped = raw
                .GroupBy(x => TimeZoneInfo.ConvertTimeFromUtc(x.Date, window.TimezoneInfo).Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            return BuildFilledDailySeries(window, grouped);
        }

        private async Task<List<DailyMetricPointDto>> BuildAttendanceSeriesAsync(
            AnalyticsWindowContext window,
            CancellationToken cancellationToken)
        {
            var raw = await _context.Attendances
                .Where(a => a.CheckInTime != null
                            && a.CheckInTime >= window.StartUtc
                            && a.CheckInTime < window.EndUtc)
                .Select(a => a.CheckInTime!.Value)
                .ToListAsync(cancellationToken);

            var grouped = raw
                .GroupBy(x => TimeZoneInfo.ConvertTimeFromUtc(x, window.TimezoneInfo).Date)
                .ToDictionary(g => g.Key, g => (decimal)g.Count());

            return BuildFilledDailySeries(window, grouped);
        }

        private static List<DailyMetricPointDto> BuildFilledDailySeries(
            AnalyticsWindowContext window,
            Dictionary<DateTime, decimal> grouped)
        {
            var startLocalDate = TimeZoneInfo.ConvertTimeFromUtc(window.StartUtc, window.TimezoneInfo).Date;
            var totalDays = Math.Max(1, (int)Math.Ceiling((window.EndUtc - window.StartUtc).TotalDays));

            var series = new List<DailyMetricPointDto>(totalDays);
            for (var i = 0; i < totalDays; i++)
            {
                var currentDate = startLocalDate.AddDays(i);
                series.Add(new DailyMetricPointDto
                {
                    Date = currentDate,
                    Value = grouped.GetValueOrDefault(currentDate, 0m)
                });
            }

            return series;
        }

        private static decimal CalculateGrowthPercent(int current, int previous)
        {
            if (previous == 0)
                return current > 0 ? 100m : 0m;

            return Math.Round((decimal)(current - previous) / previous * 100, 1);
        }

        private static string GetInitials(string firstName, string lastName)
        {
            var first = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "";
            var last = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString().ToUpper() : "";
            var initials = $"{first}{last}";
            return string.IsNullOrEmpty(initials) ? "??" : initials;
        }

        private static string GenerateAvatarColor(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "#8B5CF6";

            // Deterministic color from name hash
            var colors = new[]
            {
                "#8B5CF6", "#3B82F6", "#10B981", "#F59E0B",
                "#EF4444", "#EC4899", "#6366F1", "#14B8A6"
            };

            var hash = name.Aggregate(0, (h, c) => h + c);
            return colors[Math.Abs(hash) % colors.Length];
        }

        private sealed record AnalyticsWindowContext(
            DateTime StartUtc,
            DateTime EndUtc,
            TimeZoneInfo TimezoneInfo,
            string RequestedTimezone,
            List<string> Flags);
    }
}
