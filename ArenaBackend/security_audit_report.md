# Security Audit Report: Arena Gym Management System

**Date:** June 9, 2026
**Auditor:** Senior Application Security Engineer / Principal .NET Architect
**Project:** Arena Backend (ASP.NET Core 10, Clean Architecture)
**Scope:** ArenaAPI, ArenaMVC, ArenaApplication, ArenaDomain, ArenaInfrastructure

---

# Executive Summary

The Arena Gym Management System was subjected to a comprehensive security audit covering authentication, authorization, API security, payment integration, data protection, and infrastructure configuration.

The application demonstrates a solid **Clean Architecture** foundation with proper separation of concerns, use of modern .NET patterns, and localization support. However, several **critical security vulnerabilities** were identified that could lead to **complete system compromise**, **unauthorized access to sensitive data**, and **financial fraud**.

The most severe issues include: hardcoded secrets in source control, completely disabled authorization on critical endpoints (QR code scanning, AI chat), broken access control (IDOR) across multiple endpoints, and a payment webhook HMAC exposed as a query parameter.

**Security Score: 3.5 / 10**

| Risk Level | Count |
|------------|-------|
| Critical | 8 |
| High | 12 |
| Medium | 10 |
| Low | 5 |

---

## Critical Issues

---

### CRIT-01: Hardcoded Secrets in Source Control

**Severity:** Critical

**Location:**
- `ArenaAPI/appsettings.json` - Lines 16-19, 23-27, 35-39
- `ArenaAPI/appsettings.Development.json` - Lines 22-25, 29-34
- `ArenaAPI/Program.cs` - Lines 86-88, 148

**Problem:**
Multiple production secrets are hardcoded in plaintext in configuration files committed to the Git repository:
- **JWT Signing Key:** `"this_is_a_super_secure_key_for_arena_project_2026_very_long"`
- **Paymob API Key** (Base64-encoded but trivially decoded)
- **Paymob HMAC Secret** (used for webhook signature verification)
- **Paymob Integration ID** and **Iframe ID**
- **Gmail SMTP Password:** `"ygww nzhv moiy szrl"`
- **OpenAI API Key placeholder** (but real key expected in same file)

**Attack Scenario:**
1. An attacker gains access to the source code via a compromised developer machine, internal leak, or public exposure.
2. The attacker extracts the JWT signing key and forges arbitrary authentication tokens.
3. The attacker uses the Paymob API key to initiate fraudulent payment transactions or refunds.
4. The attacker uses the SMTP password to send phishing emails from the organization's email address.

**Impact:**
- Complete authentication bypass - attacker can impersonate any user including Admin
- Financial fraud via payment gateway compromise
- Reputational damage via email compromise
- **Business impact:** Total system compromise, financial loss, regulatory penalties (GDPR, CCPA)

**Recommended Fix:**
- Move ALL secrets to secure storage like Azure Key Vault, AWS Secrets Manager, or environment variables
- Use `dotnet user-secrets` for development
- Ensure `appsettings.json` contains only placeholder/empty values
- Rotate all compromised secrets immediately
- Audit Git history for committed secrets and use `git filter-repo` to purge

**Code Example:**
```csharp
// Program.cs — Replace direct config with Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:VaultUri"]!),
    new DefaultAzureCredential());

// For development, use user-secrets
// dotnet user-secrets set "Jwt:Key" "your-secure-key-here"
```

---

### CRIT-02: Missing Authorization on QR Code Controller

**Severity:** Critical

**Location:**
- `ArenaAPI/Controllers/QRCodeController.cs` - Lines 9-10

**Problem:**
The `[Authorize]` attribute on `QRCodeController` is **completely commented out**. This means:
- Any unauthenticated user can generate QR codes for any booking by knowing the BookingId (GUID).
- Any unauthenticated user can scan/invalidate QR codes, mark attendance, deduct subscription sessions, and change booking status to "Completed".

**Attack Scenario:**
1. An attacker calls `POST /api/qr/generate/{bookingId}` to generate a valid QR code for any booking.
2. The attacker calls `POST /api/qr/scan` with the generated code and a fabricated `ScannedById`.
3. The system marks the booking as "Completed", deducts a session from the member's subscription, and creates an attendance record.
4. The attacker can do this repeatedly to exhaust other members' subscription sessions.

**Impact:**
- Session theft - attacker can consume other members' paid sessions
- Attendance fraud - fake attendance records
- Business logic bypass - no verification that the scanner is authorized staff
- **Business impact:** Revenue loss, member dissatisfaction, inaccurate attendance records

**Recommended Fix:**
- Uncomment `[Authorize]` on the QRCodeController class
- Add role-based authorization: `[Authorize(Roles = "GymMember")]` for Generate, `[Authorize(Roles = "Admin")]` for Scan
- Verify booking ownership in `GenerateAsync` (ensure the caller owns the booking)
- Validate `ScannedById` corresponds to an actual admin user

---

### CRIT-03: Missing Authorization on AI Chat Controller

**Severity:** Critical

**Location:**
- `ArenaAPI/Controllers/AIControllers/AIController.cs` - Line 11

**Problem:**
The `[Authorize]` attribute on `ChatController` is **completely commented out**. Any unauthenticated attacker can:
- Send messages to the AI assistant with any `MemberProfileId`
- Retrieve chat history for any member
- The AI service has access to member profile data and can generate workout/nutrition plans

**Attack Scenario:**
1. An attacker calls `POST /api/chat` with any arbitrary `MemberProfileId` (GUUID enumeration).
2. The AI assistant returns personalized workout/nutrition plans and can reveal member information through prompt injection.
3. The attacker calls `GET /api/chat/history/{memberProfileId}` to read all past conversations.

**Impact:**
- Unauthorized access to sensitive member health data (weight, height, BMI, fitness goals)
- Privacy violation (health data is protected under GDPR/HIPAA-like regulations)
- **Business impact:** Legal liability, member privacy breach, reputational damage

**Recommended Fix:**
- Uncomment `[Authorize]` on ChatController
- Add ownership validation: ensure the authenticated user's MemberProfileId matches the requested one
- Implement rate limiting on AI endpoints to prevent abuse

---

### CRIT-04: Missing Authorization on User Subscriptions Controller

**Severity:** Critical

**Location:**
- `ArenaAPI/Controllers/UserSubscriptionsController.cs` - Lines 10-11

**Problem:**
The `UserSubscriptionsController` at `/api/user-subscriptions` has **NO authorization attribute** at class or method level. Any unauthenticated user can:
- `GET /api/user-subscriptions` - List all subscriptions (including other members')
- `GET /api/user-subscriptions/{id}` - View any subscription details
- `GET /api/user-subscriptions/member/{memberProfileId}` - View subscriptions by member
- `POST /api/user-subscriptions` - Create new subscriptions
- `PATCH /api/user-subscriptions/{id}/status` - Update subscription status (activate/deactivate)
- `DELETE /api/user-subscriptions/{id}` - Delete subscriptions

**Attack Scenario:**
1. An unauthenticated attacker calls `GET /api/user-subscriptions` to enumerate all subscriptions.
2. The attacker calls `PATCH /api/user-subscriptions/{id}/status` to change a subscription status from "Expired" to "Active".
3. The attacker gains free access to gym services without payment.

**Impact:**
- Complete unauthorized access to subscription data
- Privilege escalation - attacker can activate/deactivate any subscription
- Revenue loss through unauthorized subscription manipulation
- **Business impact:** Critical financial loss, data breach of all members' subscription info

**Recommended Fix:**
- Add `[Authorize(Roles = "Admin")]` at the controller level
- Add `[Authorize(Roles = "GymMember")]` with ownership checks on member-specific endpoints
- Never expose subscription CRUD without authentication

---

### CRIT-05: Missing Authorization on Payments Endpoint (GetById)

**Severity:** Critical

**Location:**
- `ArenaAPI/Controllers/PaymentsController.cs` - Line 57

**Problem:**
The `GET /api/payments/{id}` endpoint has **NO `[Authorize]` attribute**. Any unauthenticated user can view any payment by its GUID, including:
- Payment amount
- Payment method
- Payment status
- User ID and subscription information (via navigation properties loaded with `.Include(p => p.User).Include(p => p.UserSubscription).ThenInclude(s => s.Plan)`)

**Attack Scenario:**
1. An attacker discovers a valid Payment GUID (leaked in client-side code, logs, or enumeration).
2. The attacker calls `GET /api/payments/{id}` without any authentication.
3. The response includes user details, payment amounts, and subscription plan data.

**Impact:**
- Unauthorized disclosure of financial transaction data
- User information leakage (name, email, subscription details)
- **Business impact:** PCI DSS compliance issues, financial privacy violation

**Recommended Fix:**
- Add `[Authorize]` to the `GetById` method
- Add ownership check: members should only see their own payments; admins can see all

---

### CRIT-06: Insecure Direct Object Reference (IDOR) in Booking Controller

**Severity:** Critical

**Location:**
- `ArenaAPI/Controllers/BookingController.cs` - Lines 44-46
- `ArenaApplication/Services/BookingService.cs` - Lines 60-66

**Problem:**
The `GET /api/booking` endpoint accepts `memberProfileId` as a query parameter and returns all bookings for that profile. There is **no ownership verification** — any authenticated user (GymMember or Admin) can view any other member's bookings by changing the GUID.

Additionally, `CancelBooking` and `RescheduleBooking` accept a booking ID but never verify that the booking belongs to the current user (except in `CreateBookingDto` where the `MemberProfileId` is taken directly from the request body).

**Attack Scenario:**
1. An authenticated GymMember enumerates booking GUIDs for other members.
2. The attacker calls `POST /api/booking/cancel/{bookingId}` to cancel another member's booking.
3. The attacker calls `POST /api/booking/reschedule/{bookingId}` to modify another member's schedule.

**Impact:**
- Unauthorized modification of other members' bookings
- Denial of service (attacker cancels all of a competitor's gym sessions)
- **Business impact:** Member disputes, operational chaos

**Recommended Fix:**
- Extract the current user's MemberProfileId from the JWT claims (via `ICurrentUserService`)
- In `GetUserBookings`, filter by the authenticated user's profile
- In `CancelBooking` and `RescheduleBooking`, verify booking ownership before modifying

**Code Example:**
```csharp
[HttpGet]
[Authorize(Roles = "GymMember,Admin")]
public async Task<IActionResult> GetUserBookings()
{
    var memberProfileId = _currentUserService.MemberProfileId; // From JWT
    var result = await _bookingService.GetUserBookings(memberProfileId);
    // ...
}
```

---

### CRIT-07: Exposed Paymob Webhook HMAC in Query String + Insufficient Security

**Severity:** Critical

**Location:**
- `ArenaAPI/Controllers/PaymentsController.cs` - Lines 89-118
- `ArenaInfrastructure/Services/PaymobService.cs` - Lines 133-156, 158-187

**Problem:**
1. The Paymob webhook endpoint `POST /api/payments/webhook/completed` accepts the HMAC signature as a **query string parameter** (`[FromQuery] string hmac`). Query strings are logged by web servers, proxies, and load balancers, exposing the HMAC.
2. The `PaymobService.VerifyWebhookHmac` method has a critical flaw: the `obj.Order.Id` is used but the `Order` object only contains `Id` (a long). However, Paymob's HMAC is typically calculated differently according to their documentation.
3. The `MarkAsCompletedAsync` uses the `PaymentIntentId` (which is the Paymob Order ID as a string) to look up the payment, but the HMAC verification doesn't properly validate that the webhook is for the correct payment.

**Attack Scenario:**
1. An attacker intercepts the HMAC from web server logs (query string is logged).
2. The attacker replays webhook notifications with manipulated data to mark payments as completed.
3. If the HMAC verification is bypassed (or the secrets are leaked — see CRIT-01), the attacker can activate subscriptions without paying.

**Impact:**
- Fraudulent payment confirmations
- Subscription activation without payment
- **Business impact:** Direct revenue loss, financial report inaccuracies

**Recommended Fix:**
- Move HMAC to a request header (standard Paymob practice)
- Add nonce/replay protection to webhook handling
- Verify the webhook signature using the proper Paymob algorithm
- Use the transaction ID (not the order ID) for payment lookup
- Add IP whitelisting for Paymob webhook IPs

---

### CRIT-08: Weak JWT Signing Key in Source Code

**Severity:** Critical

**Location:**
- `ArenaAPI/appsettings.json` - Line 23
- `ArenaAPI/appsettings.Development.json` - Line 30
- `ArenaDomain/Shared/JWTSettings.cs` - Line 10
- `ArenaAPI/Configurations/JWTConfig/JWTConfiguration.cs` - Lines 37-38

**Problem:**
The JWT signing key is:
1. **Hardcoded** in source control (see CRIT-01)
2. A **dictionary-word based string**: `"this_is_a_super_secure_key_for_arena_project_2026_very_long"`
3. Uses **HMACSHA256** which requires a key with at least 256 bits (32 bytes) of entropy. While the string is long, it's composed of predictable words
4. The same key is used for both access token generation AND token validation

**Attack Scenario:**
1. Any developer, contractor, or attacker with repo access has the JWT signing key.
2. The attacker crafts a JWT with:
   - `ClaimTypes.Role = "Admin"` 
   - `ClaimTypes.NameIdentifier = any_guid`
   - Signs it with the exposed key
3. The attacker now has **full admin access** to the system.

**Impact:**
- Complete authentication bypass
- Privilege escalation to Admin
- Access to all API endpoints and data
- **Business impact:** Total system compromise

**Recommended Fix:**
- Generate a cryptographically random 256-bit key using `RandomNumberGenerator.GetBytes(32)`
- Store the key in Azure Key Vault or environment variables
- Rotate the signing key periodically
- Consider using asymmetric signing (RS256) with a private key stored securely

**Code Example:**
```csharp
// Generate a secure key: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
// Store in Azure Key Vault, not appsettings.json
"Jwt": {
  "Key": "",  // Empty in source control
  "Issuer": "Arena",
  "Audience": "ArenaUser"
}
```

---

## High Severity Issues

---

### HIGH-01: CORS Allows Any Origin

**Severity:** High

**Location:**
- `ArenaAPI/Program.cs` - Lines 167-173, 201

**Problem:**
The CORS policy `"AllowAll"` allows **any origin**, **any method**, and **any header**:
```csharp
options.AddPolicy("AllowAll", policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());
```

**Attack Scenario:**
1. An attacker hosts a malicious website at `evil.com`.
2. A logged-in gym member visits the attacker's site.
3. The malicious site makes authenticated AJAX requests to the Arena API (e.g., `GET /api/auth/me`, `GET /api/payments/my-payments`).
4. The attacker exfiltrates the member's data (PII, payment history, subscription details).

**Impact:**
- Cross-origin data theft
- CSRF-style attacks despite JWT authentication
- **Business impact:** PII exposure, privacy violations

**Recommended Fix:**
- Restrict CORS to specific allowed origins (the frontend application URL)
- Never use `AllowAnyOrigin()` in production
- Use `WithOrigins("https://your-frontend.com")` and `AllowCredentials()` if needed

**Code Example:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "https://your-frontend.com",
            "http://localhost:4200"  // Development only
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());  // Required for JWT with CORS
});
```

---

### HIGH-02: Plaintext Credential Logging

**Severity:** High

**Location:**
- `ArenaApplication/Services/AuthService.cs` - Lines 149-150
- `ArenaInfrastructure/AI/OpenAIService.cs` - Line 26

**Problem:**
1. In `AuthService.LoginAsync`, the user's email and password are written to `Console.WriteLine`:
   ```csharp
   Console.WriteLine(dto.Email);
   Console.WriteLine(dto.Password);
   ```
2. In `OpenAIService`, the API key is logged to console:
   ```csharp
   Console.WriteLine($"=== API KEY: {_settings.ApiKey} ===");
   ```

**Attack Scenario:**
1. An attacker gains access to server logs (e.g., via misconfigured log aggregator, container logs, or local access).
2. The attacker extracts user credentials and the AI provider API key.
3. User accounts are compromised; the API key can be used to make unauthorized LLM API calls at the organization's expense.

**Impact:**
- Credential theft for all users who log in
- API key abuse leading to financial charges
- **Business impact:** Account takeovers, reputational damage, unexpected API costs

**Recommended Fix:**
- Remove `Console.WriteLine` statements for credentials
- Use a proper logging framework (ILogger) with appropriate log levels
- Never log passwords, tokens, or API keys
- Implement log scrubbing for sensitive data

---

### HIGH-03: Exception Details Leaked in Production Responses

**Severity:** High

**Location:**
- `ArenaAPI/Controllers/SubscriptionPlansController.cs` - Lines 35, 53
- `ArenaAPI/Controllers/UserSubscriptionsController.cs` - Lines 47, 79, 100, 122, 139
- `ArenaMVC/Controllers/SubscriptionPlansController.cs` - Line 66
- Multiple other controllers

**Problem:**
Several controllers return `ex.Message` or `details = ex.Message` in HTTP responses even in production. This leaks internal implementation details, stack traces, and potentially sensitive information to attackers.

```csharp
// Example pattern found in multiple places:
catch (Exception ex)
{
    return StatusCode(StatusCodes.Status500InternalServerError, 
        new { message = _localizer["AnErrorOccurred..."], details = ex.Message });
}
```

**Attack Scenario:**
1. An attacker sends malformed requests to trigger exceptions (e.g., invalid GUIDs, SQL injection probes).
2. The error response includes exception details that reveal:
   - Database schema information
   - SQL query patterns
   - Internal server paths
   - Library versions
3. The attacker uses this information to craft more targeted attacks.

**Impact:**
- Information disclosure aiding further attacks
- Violation of OWASP Top 10 (A05:2021 - Security Misconfiguration)
- **Business impact:** Increased attack surface, compliance violations

**Recommended Fix:**
- Never return exception details in production
- Use a global exception handling middleware
- Log full exception details server-side and return generic error messages

**Code Example:**
```csharp
// Program.cs
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(
            new { error = "An internal error occurred. Please try again later." });
    });
});
```

---

### HIGH-04: No Authorization on MVC Controllers

**Severity:** High

**Location:**
- `ArenaMVC/Controllers/AdminBookingController.cs` - Line 18
- `ArenaMVC/Controllers/UserManagementController.cs` - Line 14
- `ArenaMVC/Controllers/SubscriptionPlansController.cs` - Line 10

**Problem:**
The MVC Admin controllers have **NO `[Authorize]` attribute**. Any user (authenticated or not) can access:
- `AdminBookingController/Index` - View all bookings with member names
- `AdminBookingController/Today` - View today's schedule
- `UserManagementController/Index` - List all users
- `UserManagementController/Details/{id}` - View user details
- `UserManagementController/Manage/{id}` - Change user active status
- `UserManagementController/Delete/{id}` - Soft-delete users
- `SubscriptionPlansController/Create` - Create subscription plans
- `SubscriptionPlansController/Edit` - Edit subscription plans
- `SubscriptionPlansController/Delete` - Delete subscription plans

**Attack Scenario:**
1. An unauthenticated attacker navigates to `https://arena-mvc.com/UserManagement` to view all registered users.
2. The attacker navigates to `https://arena-mvc.com/UserManagement/Manage/{id}` to deactivate a specific user.
3. The attacker navigates to `https://arena-mvc.com/UserManagement/Delete/{id}` to delete users.

**Impact:**
- Complete unauthorized access to admin functionality
- User account manipulation
- Data deletion
- **Business impact:** Total compromise of admin panel, data loss

**Recommended Fix:**
- Add `[Authorize(Roles = "Admin")]` to all admin MVC controllers
- Add `[Authorize]` to any controller that handles sensitive data
- Use a base controller with authorization for admin areas

---

### HIGH-05: IDOR in Attendance Controller

**Severity:** High

**Location:**
- `ArenaAPI/Controllers/AttendanceController.cs` - Lines 18-23
- `ArenaAPI/Controllers/AttendanceController.cs` - Lines 25-30

**Problem:**
1. `GET /api/attendance/member/{memberProfileId}` has `[Authorize]` but no ownership check — any authenticated user can view any member's attendance records.
2. `GET /api/attendance/today` has `[Authorize]` but no role restriction — any authenticated user can view today's attendance for ALL members.

**Attack Scenario:**
1. A GymMember enumerates `memberProfileId` values.
2. The member calls `GET /api/attendance/member/{otherProfileId}` to track when other members visit the gym.
3. This reveals gym attendance patterns, potentially used for stalking or competitive intelligence.

**Impact:**
- Privacy violation - members' gym attendance patterns exposed
- Stalking/harassment risk
- **Business impact:** Member safety concerns, legal liability

**Recommended Fix:**
- Restrict `GetByMember` to admin roles or the member's own profile
- Restrict `GetToday` to admin roles only
- Add ownership verification

---

### HIGH-06: IDOR in AI Chat Service

**Severity:** High

**Location:**
- `ArenaAPI/Controllers/AIControllers/AIController.cs` - Lines 30-34
- `ArenaInfrastructure/AI/ChatService.cs` - Lines 39-45

**Problem:**
The AI chat endpoint `GET /api/chat/history/{memberProfileId}` accepts a `memberProfileId` parameter without verifying the caller's identity. An authenticated attacker can view any member's chat history.

Additionally, `SendMessageAsync` uses the provided `MemberProfileId` to load member profile data (weight, height, BMI, etc.) via `UserContextBuilder.Build(profile)`, potentially exposing health data.

**Attack Scenario:**
1. GymMember A enumerates `memberProfileId` values.
2. GymMember A calls `GET /api/chat/history/{profileId_of_B}` to read B's conversations with the AI.
3. These conversations contain health data, fitness goals, dietary preferences, and personal information.

**Impact:**
- Health data privacy violation
- PII exposure
- **Business impact:** HIPAA/GDPR compliance violations, legal liability

**Recommended Fix:**
- Add `[Authorize]` back to the controller
- Validate that `memberProfileId` matches the authenticated user's profile
- Add Admin override for admin users

---

### HIGH-07: Weak Password Policy in Identity Configuration

**Severity:** High

**Location:**
- `ArenaAPI/Program.cs` - Lines 115-123

**Problem:**
The ASP.NET Core Identity password policy is configured with weak requirements:
```csharp
options.Password.RequireDigit = true;
options.Password.RequiredLength = 6;          // Too short
options.Password.RequireNonAlphanumeric = false;  // No special chars
options.Password.RequireUppercase = false;     // No uppercase
```

However, the FluentValidation `RegisterDtoValidator` enforces stricter rules (min 8 chars, uppercase, lowercase, digit, special char). The inconsistency means:
1. Admin-created users bypass FluentValidation
2. Programmatic user creation (e.g., seeding) uses Identity-only validation
3. The `ResetPasswordAsync` in AuthService uses Identity directly, bypassing FluentValidation

**Attack Scenario:**
1. An attacker attempts to brute-force user passwords.
2. Passwords can be as short as 6 characters with only digits required.
3. Dictionary attacks become feasible against shorter, simpler passwords.
4. Admin-created accounts bypass the stronger validation.

**Impact:**
- Account takeover via password brute-force
- **Business impact:** Unauthorized access to user accounts

**Recommended Fix:**
- Align Identity password policy with FluentValidation rules:
  ```csharp
  options.Password.RequiredLength = 8;
  options.Password.RequireDigit = true;
  options.Password.RequireLowercase = true;
  options.Password.RequireUppercase = true;
  options.Password.RequireNonAlphanumeric = true;
  ```
- Apply the same validation in `ResetPasswordAsync`

---

### HIGH-08: No Rate Limiting on Authentication Endpoints

**Severity:** High

**Location:**
- `ArenaAPI/Program.cs` - No rate limiting middleware configured

**Problem:**
The application has NO rate limiting on any endpoint, particularly critical authentication endpoints:
- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `POST /api/auth/refresh`

**Attack Scenario:**
1. An attacker launches a brute-force attack against `POST /api/auth/login`.
2. The attacker can make thousands of requests per second trying common passwords.
3. Without rate limiting, the attacker can eventually compromise user accounts.

**Impact:**
- Credential brute-forcing and stuffing
- Account takeover
- **Business impact:** Member account compromise, DDoS via repeated auth attempts

**Recommended Fix:**
- Implement rate limiting using `AspNetCoreRateLimit` or built-in .NET rate limiting
- Apply strict limits on auth endpoints (e.g., 5 attempts per minute per IP)
- Consider account lockout in Identity (already partially configured but rate limiting adds network-level protection)

**Code Example:**
```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

app.UseRateLimiter();
```

---

### HIGH-09: SignalR Notification Hub Has No Authentication

**Severity:** High

**Location:**
- `ArenaAPI/Hubs/NotificationHub.cs` - Lines 1-8
- `ArenaAPI/Program.cs` - Line 218

**Problem:**
The SignalR Notification Hub at `/hubs/notifications` has:
1. No `[Authorize]` attribute on the hub
2. No authentication requirement configured for SignalR
3. The `NotificationHubService.SendToUserAsync` sends notifications based on `userId` string, which could be spoofed

**Attack Scenario:**
1. An attacker connects to `wss://arena-api.com/hubs/notifications` without authentication.
2. The attacker can potentially receive notifications intended for other users.
3. The attacker can monitor real-time system activity.

**Impact:**
- Unauthorized access to real-time notifications
- Information disclosure
- **Business impact:** Privacy violation

**Recommended Fix:**
- Add `[Authorize]` to the `NotificationHub` class
- Require JWT token for SignalR connections:
  ```csharp
  app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();
  ```

---

### HIGH-10: `AllowedHosts: *` Wildcard

**Severity:** High

**Location:**
- `ArenaAPI/appsettings.json` - Line 8
- `ArenaAPI/appsettings.Development.json` - Line 8

**Problem:**
```json
"AllowedHosts": "*"
```
This wildcard allows any Host header value, making the application vulnerable to Host Header Injection attacks.

**Attack Scenario:**
1. An attacker sends a request with a malicious `Host` header (e.g., `Host: evil.com`).
2. The application generates password reset links using the attacker-controlled host.
3. The `ForgotPassword` email includes a link to `evil.com` with a valid reset token.
4. The attacker intercepts the reset token and takes over the victim's account.

**Impact:**
- Password reset poisoning
- Cache poisoning
- **Business impact:** Account takeovers, phishing attacks against members

**Recommended Fix:**
- Restrict `AllowedHosts` to specific domain values:
  ```json
  "AllowedHosts": "arena-gym.com;localhost"
  ```
- Or use ASP.NET Core's Host Filtering middleware

---

### HIGH-11: No Input Validation / Sanitization on Profile Image URL

**Severity:** High

**Location:**
- `ArenaApplication/Services/ProfileService.cs` - Line 118-119
- `ArenaApplication/Dtos/ProfileDtos/UpdateProfileDto.cs`

**Problem:**
The profile image URL is stored directly without any validation or sanitization:
```csharp
if (dto.ProfileImage is not null)
    user.MemberProfile.ProfileImageUrl = dto.ProfileImage;
```

**Attack Scenario:**
1. An attacker sets their profile image URL to a `javascript:` URL or an external tracking image.
2. When an admin visits the user management page, the browser loads the malicious URL.
3. This can lead to XSS in the admin panel or exfiltration of admin data via the image request.

**Impact:**
- Stored XSS potential
- Tracking pixels exposing admin IPs/browsers
- **Business impact:** Admin account compromise, data exfiltration

**Recommended Fix:**
- Validate the URL is a valid HTTP/HTTPS URL
- Sanitize and encode the URL when rendering
- Consider uploading images to a secure CDN with server-side validation
- Implement URL validation: `Uri.TryCreate(dto.ProfileImage, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https")`

---

### HIGH-12: Hangfire Dashboard Accessible Without Authorization

**Severity:** High

**Location:**
- `ArenaAPI/Program.cs` - Line 198

**Problem:**
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar"));
    app.UseHangfireDashboard();  // <-- No authorization
}
```
While the Hangfire dashboard is only mapped in Development, the OpenAPI/Scalar endpoint is also exposed without authentication, and the Hangfire dashboard itself has no authorization configured. If this leaks to production or a staging environment, it's critical.

**Attack Scenario:**
1. In a staging environment with `ASPNETCORE_ENVIRONMENT=Development`, the dashboard is accessible.
2. Anyone can view, trigger, or delete background jobs.
3. The dashboard reveals database connection strings and job payloads.

**Impact:**
- Background job manipulation
- Information disclosure
- **Business impact:** Service disruption, data exposure

**Recommended Fix:**
- Add authorization to Hangfire dashboard: `app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new HangfireAuthorizationFilter() } })`
- Restrict OpenAPI/Scalar to specific environments with authentication
- Never expose Swagger/OpenAPI in production

---

## Medium Severity Issues

---

### MED-01: No HTTPS in Development Environment

**Severity:** Medium

**Location:**
- `ArenaAPI/Program.cs` - Lines 203-204
- `ArenaAPI/Properties/launchSettings.json` - Line 8

**Problem:**
HTTPS redirection is only enabled in non-Development environments:
```csharp
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
```
The HTTP launch profile uses `http://localhost:5095` with no HTTPS.

**Attack Scenario:**
1. A developer works on the application over an untrusted network (coffee shop, public WiFi).
2. All traffic, including JWT tokens sent in API requests, is transmitted in plaintext.
3. An attacker on the same network captures the JWT token and uses it to impersonate the developer.

**Impact:**
- Token interception on development network
- Credential exposure
- **Business impact:** Development account compromise, leaked API keys

**Recommended Fix:**
- Enable HTTPS in all environments (use the "https" launch profile)
- Always use HTTPS redirection regardless of environment
- Use TLS certificates even in development (ASP.NET Core dev cert)

---

### MED-02: No Anti-Forgery Tokens on API Controllers

**Severity:** Medium

**Location:**
- All `ArenaAPI/Controllers/*.cs` files

**Problem:**
The API controllers use `[ApiController]` attribute but none validate anti-forgery tokens. While JWT Bearer auth is the primary protection, certain endpoints (like cookie-based scenarios or when CORS is overly permissive) remain vulnerable to CSRF.

**Attack Scenario:**
1. If a user has an active session cookie (not just JWT), an attacker can craft a malicious form.
2. The form POSTs to an API endpoint (e.g., `/api/auth/change-password`).
3. The browser automatically includes cookies, and the request is processed.

**Impact:**
- Cross-Site Request Forgery on state-changing operations
- **Business impact:** User account manipulation without their knowledge

**Recommended Fix:**
- Add `[AutoValidateAntiforgeryToken]` or `[ValidateAntiforgeryToken]` on state-changing endpoints
- Configure proper CSRF protection for the API

---

### MED-03: Console Logging Instead of Structured Logging

**Severity:** Medium

**Location:**
- `ArenaApplication/Services/AuthService.cs` - Lines 149-150
- `ArenaInfrastructure/AI/OpenAIService.cs` - Lines 26, 55-57

**Problem:**
The application uses `Console.WriteLine` for logging instead of a proper logging framework (`ILogger<T>`). This means:
- No log levels (Info, Warning, Error)
- No structured logging
- No log sinks (file, database, cloud)
- Cannot be filtered or queried

**Attack Scenario:**
1. Security incident requires investigation.
2. Logs are unstructured and scattered in console output.
3. Forensic analysis is impossible or extremely difficult.

**Impact:**
- No audit trail for security incidents
- Difficulty in debugging and monitoring
- **Business impact:** Security incident response failure

**Recommended Fix:**
- Inject `ILogger<T>` into all services
- Replace `Console.WriteLine` with `_logger.LogInformation`, `_logger.LogError`, etc.
- Configure proper logging sinks in appsettings.json

---

### MED-04: No Input Validation on Booking Dates/Times

**Severity:** Medium

**Location:**
- `ArenaApplication/Services/BookingService.cs` - Lines 41-43, 116-119

**Problem:**
The booking service validates that the date is not in the past, but:
1. No validation that `EndTime` is after `StartTime`
2. No validation of reasonable hours (e.g., a booking at 3 AM)
3. No maximum duration check
4. No double-booking prevention (two bookings at the same time for the same resource)

**Attack Scenario:**
1. An attacker creates multiple bookings for the same time slot.
2. The system accepts all of them, overbooking the gym.
3. An attacker creates a booking with `StartTime > EndTime`, causing confusion.

**Impact:**
- Operational scheduling chaos
- Double-booking leading to member complaints
- **Business impact:** Member dissatisfaction, operational inefficiency

**Recommended Fix:**
- Validate `EndTime > StartTime`
- Limit bookings to gym operating hours
- Enforce maximum booking duration
- Add unique constraint or validation to prevent overlapping bookings for the same member/time

---

### MED-05: Refresh Token Not Cryptographically Tied to Device

**Severity:** Medium

**Location:**
- `ArenaApplication/Services/TokenService.cs` - Lines 64-70
- `ArenaApplication/Services/AuthService.cs` - Lines 155-176

**Problem:**
The refresh token is a random 64-byte value stored in the database, but:
1. No device fingerprint or IP address is associated with the refresh token
2. No token family/reuse detection (if a token is used after being revoked, the entire family should be revoked)
3. Refresh tokens have a generous 7-day expiry

**Attack Scenario:**
1. An attacker steals a user's refresh token (via XSS, log, or database breach).
2. The attacker calls `POST /api/auth/refresh` from their own device.
3. The system issues a new access token without detecting the theft.
4. The legitimate user is not notified of the new login from an unrecognized device.

**Impact:**
- Persistent access even after access token expiry
- Undetected token theft
- **Business impact:** Long-term unauthorized access

**Recommended Fix:**
- Track device/IP with refresh tokens
- Implement token reuse detection (if a revoked token is used, revoke all tokens for that user)
- Send email notification on password change or new device login
- Reduce refresh token expiry

---

### MED-06: No SQL Injection Protection Analysis

**Severity:** Medium

**Location:**
- `ArenaInfrastructure/Repositories/GenericRepository.cs` - Lines 60-67

**Problem:**
The `GenericRepository.FindAsync` accepts an `Expression<Func<T, bool>>` predicate that is passed to EF Core's `Where()` method. While EF Core generally parameterizes queries, there's a risk that developers could use `FromSqlRaw` or string interpolation with this pattern in the future.

Additionally, the `PaymobService.VerifyHmac` method and `PaymentService.MarkAsCompletedAsync` use `string.Concat` to build the HMAC data string — while this isn't SQL injection, it's a fragile pattern.

**Attack Scenario:**
N/A — current EF Core usage is safe. This is a preventive finding.

**Impact:**
- Future code changes could introduce SQL injection if developers use raw SQL

**Recommended Fix:**
- Enforce the use of parameterized queries only
- Add code analysis rules to prevent raw SQL
- Use `Contains` instead of `Where` with string manipulation

---

### MED-07: ForgotPassword Leaks Email Existence via Timing

**Severity:** Medium

**Location:**
- `ArenaApplication/Services/AuthService.cs` - Lines 241-257

**Problem:**
The `ForgotPasswordAsync` method returns `Result.Success()` even when the email doesn't exist, which is good. However:
1. When the email exists, `GeneratePasswordResetTokenAsync` is called, which takes measurably longer than the null-check.
2. This timing difference can be exploited to enumerate registered email addresses.
3. The token is encoded with `WebEncoders.Base64UrlEncode` and sent via email, but the reset link includes the email as a query parameter: `resetLink = $"...&email={encodedEmail}"`.

**Attack Scenario:**
1. An attacker sends forgot-password requests for a list of potential email addresses.
2. By measuring response times, the attacker determines which emails are registered.
3. This enables targeted credential stuffing attacks.

**Impact:**
- Email enumeration
- **Business impact:** User privacy violation, targeted attacks

**Recommended Fix:**
- Add a consistent artificial delay (e.g., `Task.Delay(Random.Shared.Next(200, 500))` ) to both paths
- Do not reveal the email in the reset link URL (use the user ID or token instead)
- Consider using a constant-time comparison for the email lookup

---

### MED-08: No Account Lockout After Failed Login Attempts

**Severity:** Medium

**Location:**
- `ArenaAPI/Program.cs` - Lines 115-123

**Problem:**
The Identity configuration does NOT configure account lockout options:
```csharp
options.Lockout.AllowedForNewUsers = true;  // Default is true but not shown
options.Lockout.MaxFailedAccessAttempts = 5; // Not configured
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Not configured
```

**Attack Scenario:**
1. An attacker launches a brute-force attack against a user account.
2. No lockout occurs after multiple failed attempts.
3. The attacker can try thousands of password combinations uninterrupted.

**Impact:**
- Increased success rate for brute-force attacks
- **Business impact:** Account takeovers

**Recommended Fix:**
- Add explicit lockout configuration:
  ```csharp
  options.Lockout.AllowedForNewUsers = true;
  options.Lockout.MaxFailedAccessAttempts = 5;
  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
  ```

---

### MED-09: Password Reset Token Sent in URL

**Severity:** Medium

**Location:**
- `ArenaApplication/Services/EmailService.cs` - Line 122

**Problem:**
The password reset token is included in the reset URL link:
```
resetLink = $"{_emailSettings.FrontendUrl}/reset-password?token={encodedToken}&email={encodedEmail}";
```
When users click this link, the token is:
1. Visible in the browser address bar
2. Stored in browser history
3. Transmitted in the `Referer` header to any third-party resources loaded on the page

**Attack Scenario:**
1. A user resets their password on a shared/public computer.
2. The reset token remains in the browser history.
3. Another user accesses the history and uses the token to reset the password again.

**Impact:**
- Password reset token interception
- Account takeover
- **Business impact:** User account compromise

**Recommended Fix:**
- Use POST-based password reset (not GET)
- Request the token and email via a form POST, not URL parameters
- Consider using a one-time-use token with proper expiry and invalidation

---

### MED-10: Frontend URL Hardcoded in Callback

**Severity:** Medium

**Location:**
- `ArenaAPI/Controllers/PaymentsController.cs` - Line 125

**Problem:**
The payment callback hardcodes the frontend URL:
```csharp
var frontendCheckoutUrl = "http://localhost:4200/checkout";
```

**Attack Scenario:**
1. The callback URL is hardcoded to `localhost:4200`, which only works in development.
2. In production, users are redirected to `localhost:4200` on their machine (which likely fails).
3. An attacker could set up a local server on port 4200 to intercept the redirect.

**Impact:**
- Broken payment flow in production
- Potential phishing via localhost redirect
- **Business impact:** Payment failures, lost revenue

**Recommended Fix:**
- Read the frontend URL from configuration
- Validate the redirect URL to prevent open redirect vulnerabilities
- Use a configuration value: `_configuration["FrontendUrl"] + "/checkout"`

---

## Low Severity Issues

---

### LOW-01: Stale/Unused Debug Endpoint (WeatherForecast)

**Severity:** Low

**Location:**
- `ArenaAPI/Controllers/WeatherForecastController.cs` - Lines 1-26
- `ArenaAPI/WeatherForecast.cs`

**Problem:**
The default WeatherForecast controller and model are still present in the project. This debug endpoint is publicly accessible at `GET /weatherforecast` with no authorization.

**Attack Scenario:**
1. An attacker discovers the endpoint.
2. While not sensitive, it indicates the application was not cleaned up before production deployment.
3. This suggests other potential oversight issues.

**Impact:**
- Minor information disclosure
- Indication of incomplete cleanup

**Recommended Fix:**
- Remove the WeatherForecast controller and model
- Run a cleanup pass to remove all unused/scaffold code

---

### LOW-02: Duplicate `using` Statements

**Severity:** Low

**Location:**
- `ArenaApplication/Services/AuthService.cs` - Lines 19-20

**Problem:**
```csharp
using Microsoft.AspNetCore.WebUtilities;  // Line 19
using System.Text;                          // Line 20
// ... (duplicated later)
using Microsoft.AspNetCore.WebUtilities;  // Line 19 (duplicated)
using System.Text;                          // Line 20 (duplicated)
```

**Attack Scenario:**
N/A — minor code quality issue.

**Impact:**
- Code maintainability
- No direct security impact

**Recommended Fix:**
- Remove duplicate using statements
- Run code cleanup in IDE

---

### LOW-03: OpenAISettings Has Redundant/Misleading Field

**Severity:** Low

**Location:**
- `ArenaAPI/appsettings.json` - Line 47

**Problem:**
The `OpenAISettings` section contains a `Password` field that is not mapped in the `OpenAISettings` class and not used in any service. This is confusing and could lead to misconfiguration.

**Attack Scenario:**
N/A — but if someone puts a real password there expecting it to be used, it creates a false sense of security.

**Impact:**
- Configuration confusion
- Potential for misconfiguration

**Recommended Fix:**
- Remove the unused `Password` field from `OpenAISettings` in appsettings.json
- Update the `OpenAISettings` class if the field is needed elsewhere

---

### LOW-04: No Content Security Policy Headers

**Severity:** Low

**Location:**
- `ArenaAPI/Program.cs` - No security headers middleware

**Problem:**
The application does not set security headers such as:
- `Content-Security-Policy`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Strict-Transport-Security` (HSTS is configured in MVC but not in API)

**Attack Scenario:**
1. An attacker exploits a missing header to perform clickjacking or MIME-type sniffing attacks.
2. The API responses are vulnerable to content injection.

**Impact:**
- Increased attack surface for XSS and clickjacking

**Recommended Fix:**
- Use ASP.NET Core security headers middleware
- Add a middleware or use `app.Use(async (context, next) => { context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'"); ... })`;

---

### LOW-05: Overly Permissive Launch Profile

**Severity:** Low

**Location:**
- `ArenaAPI/Properties/launchSettings.json` - Line 8

**Problem:**
The HTTP launch profile uses `"applicationUrl": "http://localhost:5095"` without HTTPS. This is acceptable for local development but should be the HTTPS profile by default.

**Attack Scenario:**
N/A in isolation, but combined with MED-01, it's part of a pattern of weak transport security defaults.

**Impact:**
- Developer habit of testing without HTTPS

**Recommended Fix:**
- Remove the HTTP profile or make HTTPS the default
- Set `"launchBrowser": true` on the HTTPS profile only

---

# Positive Findings

Despite the security issues, the application demonstrates several good practices:

1. **Clean Architecture:** Proper separation of concerns (API, Application, Domain, Infrastructure layers) — good for maintainability and testability.

2. **Generic Repository Pattern:** Consistent data access pattern with `IGenericRepository<T, TId>`.

3. **JWT Authentication Configuration:** Proper validation of Issuer, Audience, Lifetime, and Signing Key. `ValidateLifetime = true` on the main authentication scheme.

4. **FluentValidation Integration:** Register and Login DTOs have FluentValidation validators with proper rules (even though they're stronger than Identity defaults).

5. **Refresh Token Rotation:** Old refresh tokens are revoked when new ones are issued — prevents token replay to some extent.

6. **Localization Support:** The application properly supports multilingual content (English/Arabic) with both JSON and database-backed localization.

7. **Result Pattern:** Consistent `Result<T>` pattern for service method returns, reducing unexpected exceptions.

8. **Background Job Processing:** Hangfire integration for async email notifications and reminders.

9. **Soft Delete Pattern:** `BaseEntity<T>` includes `IsDeleted` and `DeletedAt` fields for soft deletion.

10. **Email Confirmation:** OTP-based email confirmation flow for new registrations.

11. **Admin Role Separation:** `[Authorize(Roles = "Admin")]` is used on some endpoints (Payments GetAll, UpdateStatus) — a good foundation that should be extended.

12. **Query Parameter Bounds Validation:** Some controllers validate `page < 1` and `pageSize` limits.

13. **Password Hashing:** Identity's built-in password hashing (PBKDF2) is used.

14. **HTTPS in Non-Development:** HSTS and HTTPS redirection are enabled outside of development.

---

# Fix Plan

All issues are ordered by priority (severity × exploitability × business impact) and estimated implementation effort.

| Priority | Issue ID | Title | Effort | Dependencies |
|----------|----------|-------|--------|--------------|
| P0 | CRIT-01, CRIT-08 | Move secrets to secure storage, rotate keys | 2 days | Key Vault setup |
| P0 | CRIT-02 | Re-enable Authorize on QRCodeController | 0.5 day | None |
| P0 | CRIT-03 | Re-enable Authorize on AIController | 0.5 day | None |
| P0 | CRIT-04 | Add Authorize on UserSubscriptionsController | 0.5 day | None |
| P0 | CRIT-05 | Add Authorize on Payments GetById | 0.5 day | None |
| P0 | CRIT-06 | Fix IDOR in BookingController | 1 day | None |
| P0 | CRIT-07 | Fix Paymob webhook HMAC handling | 1 day | Paymob docs review |
| P1 | HIGH-01 | Restrict CORS policy | 0.5 day | None |
| P1 | HIGH-02 | Remove credential logging | 0.5 day | None |
| P1 | HIGH-03 | Global exception handling | 1 day | None |
| P1 | HIGH-04 | Add Authorize to MVC controllers | 0.5 day | None |
| P1 | HIGH-05 | Fix IDOR in AttendanceController | 0.5 day | None |
| P1 | HIGH-06 | Fix IDOR in AIController | 0.5 day | None |
| P1 | HIGH-07 | Strengthen password policy | 0.5 day | None |
| P1 | HIGH-08 | Add rate limiting | 1 day | None |
| P1 | HIGH-09 | Secure SignalR hub | 0.5 day | None |
| P1 | HIGH-10 | Restrict AllowedHosts | 0.5 day | None |
| P1 | HIGH-11 | Validate profile image URL | 0.5 day | None |
| P1 | HIGH-12 | Secure Hangfire dashboard | 0.5 day | None |
| P2 | MED-01 | Enable HTTPS in development | 0.5 day | None |
| P2 | MED-02 | Add anti-forgery tokens | 1 day | None |
| P2 | MED-03 | Replace Console logging with ILogger | 1 day | None |
| P2 | MED-04 | Add booking time validation | 1 day | None |
| P2 | MED-05 | Device fingerprinting for refresh tokens | 2 days | Design review |
| P2 | MED-06 | Add SQL injection guardrails | 0.5 day | None |
| P2 | MED-07 | Fix ForgotPassword timing/enumeration | 0.5 day | None |
| P2 | MED-08 | Add account lockout config | 0.5 day | None |
| P2 | MED-09 | Fix password reset token delivery | 1 day | UX review |
| P2 | MED-10 | Move hardcoded URL to config | 0.5 day | None |
| P3 | LOW-01 | Remove WeatherForecast endpoints | 0.5 day | None |
| P3 | LOW-02 | Clean up duplicate usings | 0.5 day | None |
| P3 | LOW-03 | Clean up config redundancy | 0.5 day | None |
| P3 | LOW-04 | Add security headers | 1 day | None |
| P3 | LOW-05 | Fix launch profile | 0.5 day | None |

**Total Estimated Effort:** 19 developer-days for P0+P1, 8.5 days for P2, 3 days for P3

**Recommended Order of Implementation:**
1. **Week 1:** All P0 (Critical) issues — secure secrets, fix auth bypasses, fix IDOR, fix payment webhook
2. **Week 2:** All P1 (High) issues — CORS, logging, exception handling, password policy, rate limiting, MVC auth
3. **Week 3:** All P2 (Medium) issues — HTTPS, CSRF, logging framework, validation improvements
4. **Week 4:** All P3 (Low) issues — cleanup, security headers

---

*Report generated on June 9, 2026. This report contains sensitive security information about the Arena Gym Management System. Distribution should be limited to authorized personnel only.*
