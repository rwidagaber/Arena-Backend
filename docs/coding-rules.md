# Arena Coding Rules

Architecture:

- Onion Architecture
- Controllers should be thin
- Business logic belongs in Services
- Repositories handle data access

Backend:

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Mapster
- FluentValidation

Dashboard:

- Use ng-apexcharts
- Reusable chart components
- Single dashboard endpoint when possible
- Admin controls and admin-facing routes belong to ArenaMVC

Security:

- JWT Authentication
- Role-based Authorization

General:

- Production-ready code only
- No mock data unless requested
- Explain database changes before implementation

Localization and Regionalization:

- Primary deployment/client context is Egypt.
- Mandatory supported locales in all projects: English (`en`) and Egyptian Arabic (`ar-EG`).
- Requirement is retroactive for existing controllers/features and required for all upcoming ones.
- API/controller responses, validation messages, notifications, and UI text must be localized.
- Use UTC in storage and render user-facing date/time in `Africa/Cairo`.
- Use `EGP` as the default displayed currency for Egypt-facing experiences.
