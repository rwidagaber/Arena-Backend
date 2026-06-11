## Plan: Arena Analytics Execution Blueprint

Implement an enterprise-grade admin analytics stack in phased, non-breaking increments across MVC admin contracts, query model, DTOs, indexing, caching, and visualization backlog. Keep compatibility with existing dashboard contract while introducing a versioned analytics contract for richer sections.

**Steps**

1. Contract-first foundation

- Introduce a dedicated admin analytics surface in ArenaMVC (controller actions that return JSON), centered on one aggregated endpoint plus focused drilldown endpoints.
- Keep existing dashboard endpoint behavior unchanged to avoid MVC regressions.
- Add explicit date window and timezone parameters to all analytics contracts.
- Keep ArenaAPI focused on shared domain/business APIs and event sources, while admin controls stay in ArenaMVC.

2. Query model architecture

- Split query workloads into two types:
  - Real-time KPIs (small windows, strict freshness)
  - Historical analytics (materialized daily/hourly facts)
- Define analytics read model boundaries independent from transactional entities.

3. DTO evolution strategy

- Preserve current AdminDashboardDto for legacy MVC home page.
- Add versioned Analytics DTOs for sectioned data blocks (executive, financial, user, operational, growth, risks, predictive).
- Add metadata envelope for freshness, timezone, and data quality flags.

4. Indexing and storage optimization

- Add operational indexes on hot filter/group columns.
- Add composite indexes for common time-series and status queries.
- Introduce analytics snapshot tables for daily/hourly aggregations.

5. Cache policy rollout

- Establish tiered cache TTL per metric class.
- Add deterministic cache keys (scope, date window, timezone, filters).
- Add invalidation rules on payment/subscription/attendance writes.

6. Visualization backlog and delivery

- Implement dashboard sections in priority waves with explicit chart specs and data contracts.
- Include only charts backed by currently available data in wave 1.
- Track blocked visualizations requiring schema additions.

7. Reliability and governance hardening

- Add lifecycle event tables and audit logs to support trustworthy analytics.
- Add data quality checks and confidence flags in responses.

8. Scalability checkpoints

- Gate each wave with performance SLOs and load targets for 10k, 100k, and 1M users.

9. Localization and Egypt regionalization rollout

- Treat Egypt as the primary client market for analytics outputs.
- Ensure all analytics contracts and dashboard labels support English (`en`) and Egyptian Arabic (`ar-EG`).
- Apply localization retroactively to existing analytics/controllers and enforce it for all new analytics features.
- Standardize user-facing rendering with `Africa/Cairo` timezone and `EGP` currency formatting.

**Relevant files**

- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaInfrastructure/Services/DashboardService.cs — legacy aggregated KPI implementation to preserve during migration.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaApplication/Dtos/Dashboard/AdminDashboardDto.cs — legacy DTO to keep backward compatible.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaAPI/Controllers/PaymentsController.cs — financial event source.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaAPI/Controllers/BookingController.cs — booking event source.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaAPI/Controllers/AttendanceController.cs — attendance event source.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaDomain/Entities/Subscription/UserSubscription.cs — subscription lifecycle dimensions.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaDomain/Entities/Payments/Payment.cs — revenue and payment quality dimensions.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaDomain/Entities/Bookings/Attendance.cs — utilization and engagement dimensions.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaDomain/Entities/Bookings/Booking.cs — booking funnel dimensions.
- d:/Learn/ITI/Final Project/Arena/ArenaBackend/ArenaMVC/Views/Home/Index.cshtml — current dashboard visualization baseline.

**Verification**

1. Every chart in backlog maps to an explicit DTO and a proven data source.
2. Every query model path has target index coverage and expected cardinality.
3. Cache policy defines freshness, key shape, and invalidation trigger.
4. Legacy MVC dashboard remains functional during rollout.
5. Load-test checkpoints validate p95 latency per phase.
6. All admin analytics responses and UI labels are available in both `en` and `ar-EG`.
7. Time and currency displays are Egypt-compliant (`Africa/Cairo`, `EGP`).

**Decisions**

- Use additive, versioned contracts rather than replacing existing dashboard DTO.
- Adopt mixed model: live KPIs plus pre-aggregated facts for trend-heavy analytics.
- Defer trainer, class, equipment, and branch analytics until schema support is added.
- Place all admin controls and admin-facing analytics endpoints in ArenaMVC.
- Enforce bilingual localization (`en`, `ar-EG`) for all existing and future analytics/controller surfaces.

**Further Considerations**

1. Timezone governance:

- Recommended: store UTC, aggregate in UTC, render in requested local timezone with explicit offset metadata.

2. Analytics storage approach:

- Recommended: internal SQL Server fact tables first, optional BI warehouse later.

3. Contract versioning policy:

- Recommended: v1 legacy dashboard, v2 analytics contract with additive deprecations only.

4. Localization governance:

- Recommended: centralize resource keys, require bilingual acceptance criteria in each analytics backlog item.
