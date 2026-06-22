# ArenaMVC Performance Review

## Summary

The MVC admin portal is well-structured overall, but has a handful of **critical issues** that should be fixed before production use, plus several medium-priority performance and reliability improvements.

---

## 🔴 Critical Issues

### 1. `IBackgroundJobService` and `AddHangfire` registered twice (`Program.cs`)

**Location**: [Program.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Program.cs#L104-L120)

```diff
- // First registration (lines 104–109)
  builder.Services.AddHangfire(config => config.UseSqlServerStorage(...));
  builder.Services.AddHangfireServer();
  builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
  builder.Services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();

- // Duplicate registration (lines 115–120)
  builder.Services.AddHangfire(config => config.UseSqlServerStorage(...));
  builder.Services.AddHangfireServer();
  builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
  builder.Services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();
```

**Problem**: `AddHangfire` is called twice, which initialises two Hangfire server instances against the same SQL Server database. This causes duplicate job processing, wasted DB connections, and subtle concurrency bugs.  
**Fix**: Remove the duplicate block (lines 115–120).

---

### 2. `DashboardService.GetDashboardDataAsync` fires 12+ sequential DB round-trips

**Location**: [DashboardService.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Services/DashboardService.cs#L27-L177)

Every admin dashboard load awaits **12 individual `CountAsync` / `ToListAsync` calls** sequentially:

| # | Query |
|---|-------|
| 1 | `Users.CountAsync` (total) |
| 2 | `UserSubscriptions.CountAsync` (active) |
| 3 | `UserSubscriptions.CountAsync` (expiring) |
| 4 | `Attendances.CountAsync` (today) |
| 5 | `Payments.SumAsync` (monthly revenue) |
| 6 | `SubscriptionPlans.ToListAsync` |
| 7 | `Users.CountAsync` (current month) |
| 8 | `Users.CountAsync` (previous month) |
| 9 | `UserSubscriptions.CountAsync` (current month) |
| 10 | `UserSubscriptions.CountAsync` (previous month) |
| 11 | `Payments.SumAsync` (previous month revenue) |
| 12 | `Attendances.ToListAsync` (weekly) |
| 13 | `Attendances.ToListAsync + Include` (recent check-ins) |

**Problem**: Each call opens a round-trip to SQL Server. On a cold start this can easily be 200–500 ms+. This is also the root cause of the `TaskCanceledException` recorded in `dashboard_error.log` — the request timeout was hit before all queries completed.

**Fixes**:
- Add **`IMemoryCache`** caching to `GetDashboardDataAsync` (the analytics v2 path already does this — `GetDashboardDataAsync` should too).
- Use `Task.WhenAll` to run independent queries in parallel (counts on different tables can overlap).
- Combine the two `Users.CountAsync` growth queries into a single query that returns both months.

```csharp
// Example: cache the dashboard
var cacheKey = $"admin-dashboard|{DateTime.UtcNow:yyyy-MM-dd-HH}";
if (_memoryCache.TryGetValue(cacheKey, out AdminDashboardDto? cached) && cached is not null)
    return cached;
// ... build dto ...
_memoryCache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
return dto;
```

---

### 3. `AdminBookingController` makes a second unbounded DB query for member profiles

**Location**: [AdminBookingController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/AdminBookingController.cs#L52-L55)

```csharp
var profiles = await _memberProfileRepo.GetAll()
    .Include(mp => mp.User)
    .Where(mp => memberProfileIds.Contains(mp.Id))
    .ToListAsync();
```

**Problem**: `GetAll()` likely starts from the full table scan before the `.Where` filter is applied. If `IGenericRepository` constructs an `IQueryable`, this is fine; if it materialises all rows first, this is a full table load per request. This pattern is duplicated in both `Index` and `Today` actions.

**Fix**: Verify `GetAll()` returns `IQueryable<T>` (not `IEnumerable<T>`). If it returns `IEnumerable`, replace with a method like `FindAsync(predicate)` that pushes the filter to SQL.

---

## 🟡 Medium Issues

### 4. `MvcAdminLoginService.ValidateAdminAsync` issues 3 DB calls per login

**Location**: [MvcAdminLoginService.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Services/MvcAdminLoginService.cs#L15-L41)

```
FindByEmailAsync → CheckPasswordAsync → GetRolesAsync
```

This is standard ASP.NET Identity behaviour but `CheckPasswordAsync` and `GetRolesAsync` each hit the DB separately. Since this is an admin-only portal with infrequent logins, this is acceptable. However, consider **short-circuit early** — the `IsActive` check happens after `FindByEmailAsync` but before password check, which is good.

No change needed for now.

---

### 5. `UserManagementController.Index` and `SearchPartial` duplicate 30+ lines of logic

**Location**: [UserManagementController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/UserManagementController.cs#L33-L122)

Both `Index` and `SearchPartial` repeat the identical ViewModel mapping block:

```csharp
var viewModels = pagedResult.Items.Select(u => new UserListViewModel { ... }).ToList();
```

**Fix**: Extract to a private helper `MapToViewModelList(IEnumerable<UserDto> items)` to reduce duplication and the risk of divergence.

---

### 6. `DashboardService` analytics `GetAnalyticsV2Async` fetches raw lists for grouping in C#

**Location**: [DashboardService.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Services/DashboardService.cs#L429-L464)

Both `BuildRevenueSeriesAsync` and `BuildAttendanceSeriesAsync` pull **all matching rows** to C# memory and then group them there:

```csharp
var raw = await _context.Payments
    .Where(...)
    .Select(p => new { Date = p.PaymentDate!.Value, p.Amount })
    .ToListAsync(cancellationToken);

// Grouped in C# memory ↓
var grouped = raw.GroupBy(x => TimeZoneInfo.ConvertTimeFromUtc(x.Date, window.TimezoneInfo).Date)
```

This is done intentionally to support timezone conversion (which EF Core can't do in SQL), but for large datasets this transfers unnecessary data to the app server. The 3-minute/15-minute cache mitigates this, but the first request after a cache miss is still expensive.

**Consideration**: For windows > 90 days, add a warning or enforce a shorter cap (currently capped at 366 days).

---

### 7. `HomeController.Index` error handler writes to a hardcoded flat file path

**Location**: [HomeController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/HomeController.cs#L47-L50)

```csharp
System.IO.File.WriteAllText("dashboard_error.log", ex.ToString());
```

**Problems**:
- The relative path resolves to the application's working directory, which may not be writable in production.
- Only the **last** error is stored (file is overwritten, not appended).
- Error silently discarded if the file write fails (`catch {}`).

**Fix**: Use the ASP.NET `ILogger<HomeController>` already injected into most controllers — or inject it here — to log to the configured logging provider instead.

```csharp
// Inject via constructor
private readonly ILogger<HomeController> _logger;

// In catch block
_logger.LogError(ex, "Failed to load admin dashboard data");
```

---

## 🟢 Minor / Good Practices Already in Place

| Area | Assessment |
|------|-----------|
| **Auth** | Cookie auth uses `HttpOnly`, `SlidingExpiration`, proper 8-hour lifetime ✅ |
| **AntiForgery** | All POST actions have `[ValidateAntiForgeryToken]` ✅ |
| **Soft-delete** | All queries filter `!IsDeleted` correctly ✅ |
| **Pagination** | Page size is bounded (max 100) in all list endpoints ✅ |
| **Analytics caching** | `GetAnalyticsV2Async` uses versioned `IMemoryCache` ✅ |
| **No-op hub** | `NoopNotificationHub` avoids SignalR overhead in admin UI ✅ |
| **Open redirect** | `Url.IsLocalUrl(returnUrl)` used to prevent open-redirect attacks ✅ |
| **Dead code** | `UserSubscriptionsController` is fully commented out — clean it up or delete the file |

---

## Recommended Fix Priority

| Priority | Issue | Effort |
|----------|-------|--------|
| 🔴 Fix now | Duplicate Hangfire/DI registrations in `Program.cs` | 2 min |
| 🔴 Fix now | Cache `GetDashboardDataAsync` (fix the `TaskCanceledException`) | 30 min |
| 🟡 Soon | Parallelise independent dashboard DB queries with `Task.WhenAll` | 1 hr |
| 🟡 Soon | Replace flat-file error log with `ILogger` in `HomeController` | 15 min |
| 🟡 Soon | Verify `GetAll()` returns `IQueryable` in `AdminBookingController` | 15 min |
| 🟢 Later | Extract `MapToViewModelList` helper in `UserManagementController` | 10 min |
| 🟢 Later | Delete or restore `UserSubscriptionsController.cs` | 5 min |
