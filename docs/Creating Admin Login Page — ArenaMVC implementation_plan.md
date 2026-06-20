# Admin Login Page — ArenaMVC

Add a secure, styled admin login page to the ArenaMVC project that visually mirrors the Angular frontend's login design and integrates with the existing `IAuthService.LoginAsync()` service. The page must protect all admin routes, support light/dark themes, and be fully localized in en-US and ar-EG.

---

## Key Findings from Research

| Area | Finding |
|---|---|
| **Auth service** | `IAuthService.LoginAsync(UserloginDto)` already exists in `ArenaApplication`. It is **not registered** in `ArenaMVC/Program.cs` yet — it must be added. |
| **Identity / Cookie auth** | `ArenaMVC` does **not** call `AddAuthentication` / `UseAuthentication`. Both must be wired up. The API project uses JWT; the MVC project needs **Cookie-based auth**. |
| **Admin role** | `DataSeeder` seeds `admin@arena.com / Admin@123456` with the `"Admin"` role. Role verification is done through `UserManager` (Identity). |
| **No `[Authorize]`** | No existing MVC controllers use `[Authorize]`. They will all be protected after this change. |
| **`_Layout.cshtml`** | Has sidebar + header. The login page must use a **different layout** (`_LoginLayout.cshtml`) that hides the sidebar/header — identical to how the Angular app positions the card. |
| **Localization** | DB-backed `TranslationSeeder` + `DbStringLocalizerFactory` already in use. New keys are added to `en-US.json` and `ar-EG.json`. `_LanguageSwitcher` partial reusable as-is. |
| **Theme** | `localStorage` + `data-theme` attribute already implemented in `_Layout.cshtml`. The login page must replicate the same inline script and toggle logic. |
| **CSS design system** | `StyleSheet.css` has all CSS variables (`--bg-color`, `--card-bg`, `--accent-color`, etc.) already covering light/dark modes and RTL. |
| **Frontend reference** | Angular login: frosted glass card (~480 px, left side), background image on the right, border on card edge, `#418C27` green accent. |

---

## Proposed Changes

### 1. Service Registration — `Program.cs`

#### [MODIFY] [Program.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Program.cs)

- Register `IAuthService, AuthService` (already implemented in `ArenaApplication.Services`).
- Register all its dependencies that are missing from MVC:
  - `UserManager<ApplicationUser>` → requires `AddIdentity` or `AddIdentityCore`.
  - `IAuthRepository, AuthRepository`
  - `ITokenService, TokenService`
  - `IOtpService, OtpService`
  - `IGoogleTokenValidator, GoogleTokenValidator`
  - `JWTSettings` configuration binding.
- Add **Cookie Authentication**:
  ```csharp
  builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
      .AddCookie(options => {
          options.LoginPath = "/Auth/Login";
          options.AccessDeniedPath = "/Auth/AccessDenied";
          options.ExpireTimeSpan = TimeSpan.FromHours(8);
          options.SlidingExpiration = true;
      });
  ```
- Add `app.UseAuthentication()` **before** `app.UseAuthorization()`.
- Wire up ASP.NET Identity for `UserManager` (read-only, no sign-in manager needed beyond cookie).

---

### 2. Auth Controller — `[NEW]`

#### [NEW] [AuthController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/AuthController.cs)

Handles all authentication routes for the MVC admin panel.

```
GET  /Auth/Login              → renders login view (redirect to Home if already authenticated)
POST /Auth/Login              → validates credentials, checks Admin role, issues cookie, redirects
GET  /Auth/Logout             → signs out, clears cookie, redirects to /Auth/Login
GET  /Auth/AccessDenied       → renders access denied view
```

**Logic for `POST /Auth/Login`:**
1. Call `IAuthService.LoginAsync(dto)` — reuses the existing service directly.
2. If login fails → show model error.
3. If the user's role is **not** `"Admin"` → reject with localized "Unauthorized" error.
4. On success → create `ClaimsPrincipal` from the returned `AuthResponseDto`, issue a cookie via `HttpContext.SignInAsync`, then redirect to `returnUrl` or `/`.

> **Note:** `IAuthService.LoginAsync` uses `UserManager` internally. The MVC app will call it as a plain service (no HTTP call). The JWT token returned is discarded — the MVC admin uses cookie auth only.

---

### 3. Login ViewModel — `[NEW]`

#### [NEW] [AdminLoginViewModel.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Models/AdminLoginViewModel.cs)

```csharp
public class AdminLoginViewModel {
    [Required] [EmailAddress] public string Email { get; set; }
    [Required] [DataType(DataType.Password)] public string Password { get; set; }
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
```

---

### 4. Login Layout — `[NEW]`

#### [NEW] [_LoginLayout.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/Shared/_LoginLayout.cshtml)

A stripped-down layout (no sidebar, no header nav) that:
- Includes the same theme inline `<script>` (localStorage → `data-theme`).
- Loads Bootstrap, Lucide, `StyleSheet.css`, and a new `admin-login.css`.
- Loads the `_LanguageSwitcher` partial in the top-right corner (floating).
- Renders a theme toggle button (floating top-right, alongside the language switcher).
- Full-viewport flex container, mirroring the Angular layout structure.

---

### 5. Login Views — `[NEW]`

#### [NEW] Views/Auth/ directory

**`Login.cshtml`** — Uses `_LoginLayout`. Mirrors the Angular login card visually:
- Left panel: glassmorphism card with email, password (show/hide toggle), remember-me, sign-in button, and server error banner.
- Right side: CSS background image (positioned via `::before` pseudoelement in CSS).
- All labels, placeholders, and button text are localized via `@Localizer[...]`.
- Form `dir` attribute set from `CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft`.

**`AccessDenied.cshtml`** — Simple centered card with localized message and a "Back to Login" link. Uses `_LoginLayout`.

---

### 6. Login CSS — `[NEW]`

#### [NEW] [admin-login.css](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/wwwroot/css/admin-login.css)

Closely modeled on Angular's `login.css`. Key rules:
- `body` → full-viewport flex, background-image `man.png` (right-aligned, cover).
- `html[data-theme="light"] body` → `man-light.png` variant.
- `.login-card` → frosted-glass, `max-width: 480px`, 100vh tall, left-docked, `backdrop-filter: blur(20px)`, right-border accent, padding `40px 60px`, flex-column.
- Input focus → `border-color: var(--accent-color)` + glow ring.
- Submit button → `background: var(--accent-color)`, pill shape, hover lift.
- RTL: card border switches sides; back-arrow flips; text aligns right.
- Responsive (≤900px): card goes full-width.
- Floating controls: `.login-controls` positioned `fixed top-right` for theme toggle + language switcher.

---

### 7. Authorize All Existing Controllers

#### [MODIFY] All existing MVC controllers

Add `[Authorize(Roles = "Admin")]` attribute to all controller classes:
- [AdminAnalyticsController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/AdminAnalyticsController.cs)
- [AdminBookingController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/AdminBookingController.cs)
- [HomeController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/HomeController.cs)
- [SubscriptionPlansController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/SubscriptionPlansController.cs)
- [UserManagementController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/UserManagementController.cs)
- [UserSubscriptionsController.cs](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Controllers/UserSubscriptionsController.cs)

> **Note:** The `SetCulture` POST action in `HomeController` must be left **without** `[Authorize]` (or exempted with `[AllowAnonymous]`) so language can be switched on the login page itself.

---

### 8. Logout Link in Sidebar

#### [MODIFY] [_Layout.cshtml](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaMVC/Views/Shared/_Layout.cshtml)

Add a "Logout" nav-item at the bottom of `.sidebar-nav` that posts to `GET /Auth/Logout`.

---

### 9. Localization Keys

#### [MODIFY] [en-US.json](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Resources/en-US.json)
#### [MODIFY] [ar-EG.json](file:///d:/Learn/ITI/Final%20Project/Arena/ArenaBackend/ArenaInfrastructure/Resources/ar-EG.json)

New keys to add:

| Key | en-US | ar-EG |
|---|---|---|
| `AdminLogin` | `Admin Login` | `تسجيل دخول المشرف` |
| `AdminLoginSubtitle` | `Sign in to access the Arena admin panel` | `سجّل دخولك للوصول إلى لوحة تحكم أرينا` |
| `EmailAddress` | *(already exists)* | *(already exists)* |
| `Password` | `Password` | `كلمة المرور` |
| `RememberMe` | `Remember me` | `تذكّرني` |
| `ForgotPassword` | `Forgot password?` | `نسيت كلمة المرور؟` |
| `SignIn` | `Sign In` | `تسجيل الدخول` |
| `AdminOnlyAccess` | `This portal is for administrators only.` | `هذه البوابة للمشرفين فقط.` |
| `AccessDenied` | `Access Denied` | `الوصول مرفوض` |
| `BackToLogin` | `Back to Login` | `العودة إلى تسجيل الدخول` |
| `Logout` | `Logout` | `تسجيل الخروج` |

---

## Design Notes

### Visual Design Spec (matching Angular)
```
┌──────────────────┬───────────────────────────────────────────┐
│  Login Card      │  Background Image                         │
│  max-w: 480px    │  man.png / man-light.png (cover, right)  │
│  glass effect    │                                           │
│  100vh tall      │  [floating: 🌙 + 🌐 top-right]           │
│                  │                                           │
│  [Arena Logo]    │                                           │
│  Admin Login     │                                           │
│  Subtitle        │                                           │
│                  │                                           │
│  [📧 Email]      │                                           │
│  [🔑 Password 👁]│                                           │
│  [☑ Remember me] [Forgot?]                                  │
│                  │                                           │
│  [Error Banner]  │                                           │
│  [Sign In Btn]   │                                           │
└──────────────────┴───────────────────────────────────────────┘
```

### Color Palette (from existing CSS variables)
- **Dark mode**: `--bg-color: #0F0F11`, `--card-bg: #16161A`, `--accent-color: #d5eb45`
- **Light mode**: `--bg-color: #e6e7e2`, `--card-bg: #FFFFFF`, `--accent-color: #C6EF2E`
- **Button**: filled with `--accent-color`, text `--black` (dark on bright)
- **Card border**: `var(--border-color)` on the edge facing content

> The Angular frontend uses `#418C27` green. The MVC admin uses the existing neon yellow-green (`--accent-color`) to stay consistent with the admin design system already established.

---

## Dependency Chain

```
IAuthService.LoginAsync
  └── UserManager<ApplicationUser>       → AddIdentityCore
  └── IAuthRepository, AuthRepository
  └── ITokenService, TokenService
  └── IOtpService, OtpService
  └── IBackgroundJobService              → already registered
  └── IGoogleTokenValidator, GoogleTokenValidator
  └── JWTSettings                        → appsettings.json binding
```

> [!IMPORTANT]
> The `IAuthService` uses `JWTSettings` to create refresh tokens — even though the MVC app will not use JWTs. This is a side-effect of reusing the service as-is. The JWT config must be present in `appsettings.json`. Alternatively, a **lightweight `MvcAdminLoginService`** can be created that only calls `UserManager.CheckPasswordAsync` + `UserManager.GetRolesAsync` and does not touch tokens. **Please confirm which approach you prefer (Option A or Option B below).**

---

## Open Questions

> [!IMPORTANT]
> **Option A — Reuse `IAuthService.LoginAsync`** (full service)
> Register all of `IAuthService`'s dependencies in MVC. Cleaner long-term; reuses existing, tested code. Adds ~5 dependency registrations. JWT tokens are generated but discarded.
>
> **Option B — Lightweight `MvcAdminLoginService`** (minimal)
> Create a small new service in `ArenaMVC/Services/` that only calls `UserManager.CheckPasswordAsync` and `GetRolesAsync`. Avoids JWT dependency complexity in MVC. Requires ~2 new registrations (`UserManager` + the service itself).
>
> 👉 **Recommendation: Option B** — cleaner separation, avoids token-generation side effects in an MVC context.

> [!NOTE]
> The background image `man.png` / `man-light.png` is referenced from the Angular frontend. Should the MVC login use the **same images** (copied to `wwwroot/images/`) or a **different background**? If using the same images, they need to be copied.

---

## Verification Plan

### Automated Tests
- `dotnet build` — ensures no compilation errors after new registrations.

### Manual Verification
1. Navigate to `https://localhost:{port}/` → should redirect to `/Auth/Login`.
2. Submit invalid credentials → localized error message appears.
3. Submit non-admin user credentials → "Admin only" error.
4. Submit `admin@arena.com / Admin@123456` → redirects to Home dashboard.
5. Toggle theme on login page → card and background switch between dark/light.
6. Switch language to ar-EG → all text flips to Arabic, card switches RTL (border side flips, inputs align right).
7. Resize to mobile (≤900px) → card goes full-width, readable.
8. Click Logout in sidebar → returns to login page.
9. Navigate to any protected URL after logout → redirects to `/Auth/Login?ReturnUrl=...`.
