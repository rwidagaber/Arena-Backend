# ArenaMVC Admin Dashboard - Findings

## Inspection Status

- Controllers: ✓ Examined
- Models/ViewModels: ✓ Examined
- Services: ✓ Examined
- Views: ✓ Examined
- CSS: ✓ Examined
- wwwroot/JS: ✓ Examined
- Auth/Roles: ✓ Checked

## Quick Summary

- MVC app is admin-only UI (no auth checks on MVC itself)
- Admin controls ownership: all admin-facing controls and routes should stay in ArenaMVC
- Lightweight services for MVC only (DashboardService, UserManagementService, BookingService)
- No chart libraries (ApexCharts/ChartJS not installed)
- Uses inline SVG for basic charting (primitive line charts)
- No Areas folder present
- Localization enabled (en-US, ar-EG)
- Responsive Bootstrap + custom CSS (modern dark theme)
- Color scheme: Dark mode with yellow accent (#d5eb45), teal secondary
- Sidebar navigation with locale switcher

## Controllers (No Auth/Authorization attributes)

1. HomeController - Dashboard main view
2. AdminBookingController - Booking CRUD + Today's schedule
3. UserManagementController - User search, details, manage actions
4. SubscriptionPlansController - Plan CRUD
5. UserSubscriptionsController - COMMENTED OUT

## Current Dashboard Cards (Home/Index)

KPIs: Total Members, Active Subscriptions, Monthly Revenue, Active Plans, Expiring Subscriptions, Today's Attendance
Chart: Weekly attendance (Mon-Sun) - SVG line chart
Live feed: Recent check-ins (5 most recent) with member avatars
Shortcuts: Create plan, view plans, privacy

## Data Sources

- DashboardService (Infrastructure): Queries AppDbContext for:
  - Total members (non-deleted users)
  - Active subscriptions count
  - Expiring subscriptions (7-day window)
  - Today's attendance
  - Monthly revenue (Payments table)
  - Plans (active/total)
  - Growth metrics (member/subscription/revenue MoM %)
  - Weekly attendance (Mon-Sun grouping)
  - Recent check-ins (last 5 attendances with member/plan info)

## Admin Features Implemented

- Booking management with filters (status, date)
- Today's bookings view
- User management with search, detail view, manage/delete actions
- Subscription plans CRUD
- User details with subscription history
- No trainer management UI
- No AI analytics UI
- No occupancy/heatmap UI

## Localization Coverage Requirement (Egypt)

- Client context is Egypt; admin experience must support English (`en`) and Egyptian Arabic (`ar-EG`).
- Existing/past admin controllers and views in ArenaMVC must be localized retroactively.
- Upcoming admin controllers and features must include bilingual localization before release.
- Localize dashboard KPIs, chart legends, filter labels, validation messages, and notification text.
- Use Egypt-aware display rules in admin UI: `Africa/Cairo` for date/time and `EGP` for currency.
