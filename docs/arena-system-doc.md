# **ARENA AI-Powered Gym Management System**

## System Documentation · v2.0

`ASP.NET Core Web API | ASP.NET MVC | Angular | OpenAI API | Paymob | SQL Server | JWT Auth`

---

## Table of Contents

1. Project Overview
2. System Users
3. Main Features
4. Complete User Flows
5. Architecture Overview
6. System Architecture (Detailed)
7. Database Entities
8. Technologies Used
9. Background Services
10. Security
11. APIs Overview
12. AI Workflow
13. Localization & Egypt Regionalization

---

## 1. Project Overview

### Project Name

Arena — AI-Powered Gym Management System

### What is Arena?

Arena is an intelligent gym management platform that combines traditional gym management with AI-powered personalization. The system automates operations such as subscriptions, bookings, attendance, and fitness planning.

### Key Capabilities

| Gym Member (Angular App) | Admin (ASP.NET MVC Dashboard) |
| ------------------------ | ----------------------------- |
| Subscribe to gym plans   | Manage subscriptions          |
| Book gym sessions        | Manage members                |
| Generate workout plans   | Track attendance              |
| Generate nutrition plans | Manage trainers               |
| Track progress           | View analytics                |
| AI chatbot assistance    | Send notifications            |

---

## 2. System Users

### 2.1 Gym Member

- Register and login
- Subscribe to gym plans
- Use AI chatbot features
- Book gym sessions
- Receive QR attendance codes
- Track workouts and nutrition
- Monitor progress

### 2.2 Admin

- Manage members
- Manage subscriptions and plans
- Manage trainers
- Scan QR attendance
- View analytics dashboard
- Send notifications

---

## 3. Main Features

### Authentication & Authorization

- JWT authentication
- Role-based access (Admin / GymMember)
- ASP.NET Identity security

### Subscription System

- Multiple plans (Monthly / Quarterly / Yearly)
- Session tracking
- Auto-expiry handling

### Payment System

- Paymob / Stripe integration
- Payment verification workflow

### AI Chatbot

- Conversational assistant
- Context-aware responses
- Booking via natural language

### Workout & Nutrition AI

- Personalized plans
- Goal-based generation
- Structured schedules

### Booking System

- AI-powered booking flow
- Smart slot allocation
- QR generation

### Attendance System

- QR scanning by admin
- Automatic session deduction

### Notifications

- Subscription reminders
- Booking alerts
- System updates

---

## 4. Complete User Flows

### Registration Flow

1. User registers
2. Data validated
3. Account created
4. JWT issued
5. User logged in

### Subscription Flow

1. User selects plan
2. Payment initiated
3. Payment confirmed
4. Subscription activated

### Booking Flow

1. User chats with AI
2. Intent detected
3. Slot validated
4. Booking created
5. QR generated

---

## 5. Architecture Overview

The system is split into three main applications:

### 1. Gym Member Application (Angular)

- Handles all user-facing features
- Subscription, booking, AI chat, progress tracking

### 2. Admin Dashboard (ASP.NET MVC)

- Used by gym staff and owners
- Member management, attendance, analytics
- All admin controls and admin-facing routes are implemented in this ArenaMVC project

### 3. Backend API (ASP.NET Core Web API)

- Shared backend for both apps
- Handles business logic, AI, authentication, payments
- Provides shared/member/integration APIs and domain services; it is not the host for admin controls

---

## 6. System Architecture (Detailed)

### Architecture Style: Onion Architecture

The system follows Onion Architecture to ensure separation of concerns and scalability.

### Layers

1. Angular Frontend (Gym Members)
2. ASP.NET MVC (Admin Dashboard)
3. ASP.NET Core Web API
4. AI Service Layer
5. Business Services Layer
   - Booking Service
   - Subscription Service
   - Workout Service
   - Nutrition Service
   - Notification Service
6. Repository Layer (EF Core)
7. SQL Server Database

### Key Principle

The AI layer never accesses the database directly.  
All operations go through business services.

---

## 7. Database Entities

- User & Roles → ApplicationUser, Role
- Subscription → SubscriptionPlan, UserSubscription, Payment
- Booking → Booking, Attendance
- Workout → WorkoutPlan, WorkoutDay, Exercise
- Nutrition → NutritionPlan, Meal
- AI → ChatMessage, Notification
- Tracking → HealthMetric
- Gym → Trainer

---

## 8. Technologies Used

### Frontend

- Angular (Gym Member App)
- Bootstrap / Tailwind CSS

### Admin

- ASP.NET MVC

### Backend

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Identity
- JWT Authentication
- FluentValidation

### AI

- OpenAI API
- Prompt Engineering

### External Services

- Paymob
- Stripe
- Cloudinary
- QR Code Generator

---

## 9. Background Services

### Subscription Expiration Service

- Auto-check expired subscriptions
- Send reminders
- Disable expired accounts

### Booking Reminder Service

- Sends reminders before sessions
- Reduces no-shows

---

## 10. Security

- JWT authentication
- Role-based authorization
- Password hashing (ASP.NET Identity)
- Input validation (FluentValidation)
- Secure payment gateways

---

## 11. APIs Overview

Admin controls policy:

- Admin-facing controls and routes belong to ArenaMVC.
- ArenaAPI is used for shared business APIs and workflow endpoints.

### Auth

- POST /api/auth/register
- POST /api/auth/login

### Subscription

- GET /api/plans
- POST /api/payments/create
- POST /api/payments/confirm

### Booking

- POST /api/bookings
- GET /api/bookings

### Attendance

- POST /api/attendance/scan

### AI Chat

- POST /api/chat
- GET /api/chat/history

### Workout & Nutrition

- GET /api/workouts/current
- GET /api/nutrition/current

### Notifications

- GET /api/notifications

---

## 12. AI Workflow

### Inputs

- User profile
- Chat history
- Subscription status
- Booking history

### Outputs

- Workout plans
- Nutrition plans
- Booking actions
- Fitness insights

### Flow

1. User sends message
2. AI interprets intent
3. Backend service selected
4. Action executed
5. Response returned

---

## Conclusion

Arena is a scalable AI-powered gym system combining:

- Angular for members
- ASP.NET MVC for admins
- ASP.NET Core Web API as backend
- OpenAI for intelligence

This separation ensures scalability, maintainability, and clean architecture.

---

## 13. Localization & Egypt Regionalization

Arena is deployed for a client in Egypt, so localization is a core system requirement.

- Mandatory locales across all projects: English (`en`) and Egyptian Arabic (`ar-EG`).
- Scope includes ArenaAPI, ArenaApplication, ArenaInfrastructure, ArenaMVC, and the Angular member app.
- Requirement is retroactive: existing/past controllers and features must be localized.
- Requirement is proactive: every new/upcoming feature and controller must ship with both locales.
- API and MVC controllers should honor `Accept-Language` (and optional explicit culture input) with deterministic fallback.
- Store timestamps in UTC; render user-facing date/time using Egypt timezone (`Africa/Cairo`).
- Use Egypt-friendly currency formatting (`EGP`) for plans, payments, and dashboard analytics.
- Localize all user-facing messages: validation, business errors, notifications, emails, and dashboard labels.
