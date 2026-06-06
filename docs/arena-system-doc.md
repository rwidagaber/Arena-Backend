# ARENA AI-Powered Gym Management System System Documentation · v1.

```
ASP.NET Core Angular OpenAI API Paymob SQL Server JWT Auth
```

### Table of Contents

1. Project Overview
2. System Users
3. Main Features
4. Complete User Flows
5. System Architecture
6. Database Entities
7. Technologies Used
8. Background Services
9. Security
10. APIs Overview
11. AI Workflow

## 1. Project Overview

#### Project Name

Arena — AI-Powered Gym Management System

#### What is Arena?

Arena is an intelligent gym management platform that combines traditional gym management with
AI-powered personalized fitness assistance. The platform focuses on automation, personalization, and
delivering an intelligent gym experience.

#### Key Capabilities

```
Gym Member Admin
```

- Subscribe to gym plans • Manage subscriptions and plans
- Book gym sessions • Manage gym members
- Generate personalized workout & nutrition
  plans • Track attendance
- Track attendance and fitness progress • Monitor bookings
- Receive notifications and reminders • Manage trainers
- Use AI chatbot assistance • View analytics and reports

## 2. System Users

#### 2.1 Gym Member

- Register and login to the platform
- Subscribe to available gym plans
- Access AI-powered features after subscription
- Book gym sessions via AI chatbot
- Receive QR codes for attendance check-in
- Generate personalised workout plans
- Generate personalised nutrition plans
- Chat with AI fitness assistant
- Track fitness progress and health metrics
- Receive smart notifications and reminders

#### 2.2 Admin

- Manage gym members and their profiles
- Create and manage subscription plans
- Manage trainer roster
- Scan QR codes for attendance
- View and monitor all bookings
- Access analytics and reporting dashboard
- Monitor active subscriptions
- Send notifications to members

## 3. Main Features

#### 3.1 Authentication & Authorization

- User registration & login
- JWT-based authentication
- Role-based authorization (Admin / GymMember)
- Secure password hashing via ASP.NET Identity
  ASP.NET Identity JWT Auth Role-Based Access

#### 3.2 Subscription System

- Multiple subscription plans (Monthly / Quarterly / Yearly)
- Session limit tracking per plan
- Subscription activation after payment
- Expiration handling & feature locking
  Subscription Plans Session Limits Auto Expiry

#### 3.3 Online Payment System

- Paymob & Stripe gateway integration
- Payment verification workflow
- Full payment history & transaction tracking
- Subscription activated on successful payment
  Paymob Stripe Payment Verification

#### 3.4 AI Chatbot

- Conversational AI fitness assistant
- Personalised workout and nutrition guidance
- Gym booking through natural language
- User profile-aware context responses
  OpenAI API Prompt Engineering Context-Aware

#### 3.5 Workout Plan Generation

- AI-generated weekly workout schedules
- Personalised by goal (weight loss / muscle gain / etc.)
- Structured: Plan → Days → Exercises with sets & reps
- Multiple difficulty levels
  AI Generated Goal-Based Weekly Schedule

#### 3.6 Nutrition Plan Generation

- Daily calorie calculation
- Macro tracking (Protein / Carbs / Fats)
- Personalised meal recommendations
- Goal-aware nutritional targets
  AI Generated Macro Tracking Personalised

#### 3.7 AI-Powered Booking System

- Conversational natural-language booking
- Smart slot recommendations
- Automatic booking validation and QR generation
- Reschedule and cancel via AI chat
  Booking Agent QR Code Intent Detection

#### 3.8 QR Attendance System

- QR code generated per booking
- Admin scans QR on member arrival
- Attendance recorded & session deducted automatically
  QR Generation Admin Scan Auto Deduct

#### 3.9 Notifications System

- Subscription expiration reminders
- Booking confirmation & reminders
- Health alerts and announcements
- Types: Info / Warning / Success / Error
  Push Notifications Smart Reminders

#### 3.10 Progress Tracking

- Workout & attendance history tracking
- Nutrition log monitoring
- Health metrics dashboard
- User progress overview
  Progress Dashboard Health Metrics

## 4. Complete User Flows

#### 4.1 User Registration

##### 1 User opens the website

##### 2 User clicks Register

##### 3 User enters personal information

##### 4 System validates the data

##### 5 User account created in database

##### 6 JWT token generated

##### 7 User is logged in automatically

#### 4.2 Subscription Purchase

##### 1 User views available subscription plans

##### 2 User selects desired plan

##### 3 Payment process initiated

##### 4 User completes payment

##### 5 Subscription activated

##### 6 AI features unlocked

#### 4.3 AI Gym Booking

##### 1 User opens AI chatbot

##### 2 User sends natural language booking request

##### 3 AI detects booking intent

##### 4 AI extracts date and time information

##### 5 System validates active subscription

##### 6 System checks remaining sessions

##### 7 System checks gym capacity

##### 8 Booking entity created in database

##### 9 QR code generated and stored

##### 10 Booking confirmation returned to user

##### 11 QR code displayed in user profile

#### 4.4 Attendance Recording

##### 1 Member arrives at the gym

##### 2 Admin scans member QR code

##### 3 System validates the booking

##### 4 Attendance record created

##### 5 Remaining sessions updated

#### 4.5 Workout Generation

##### 1 User requests a workout plan

##### 2 AI receives full user profile data

##### 3 AI generates structured weekly plan

##### 4 Plan saved to database

##### 5 User views plan in profile dashboard

## 5. System Architecture

#### Architecture Style: Onion Architecture

Arena follows the Onion Architecture pattern, ensuring clear separation of concerns, high testability, and
independence from infrastructure details. The AI layer does not access the database directly — it
communicates exclusively through business services.

##### 1 User / Angular Frontend

##### 2 API Controllers (ASP.NET Core Web API)

##### 3 AI Service (Intent Detection & Orchestration)

##### 4 Business Services (Booking / Workout / Subscription / Notification)

##### 5 Repository Layer (Entity Framework Core)

##### 6 SQL Server Database

## 6. Database Entities

```
User & Authentication ApplicationUser, Roles
```

```
Subscription System SubscriptionPlan, UserSubscription, Payment
```

```
Booking & Attendance Booking, Attendance
```

```
Workout System WorkoutPlan, WorkoutDay, WorkoutExercise, Exercise,
WorkoutLog
```

```
Nutrition System NutritionPlan, Meal, MealLog
```

```
AI & Communication ChatMessage, Notification
```

```
Tracking HealthMetric
```

```
Gym Management Trainer
```

## 7. Technologies Used

#### Backend

```
ASP.NET Core Web API Entity Framework Core SQL Server ASP.NET Identity
```

```
JWT Authentication FluentValidation Mapster
```

#### Frontend

```
Angular Bootstrap / Tailwind CSS
```

#### Admin Panel

```
ASP.NET MVC
```

#### AI

```
OpenAI API Prompt Engineering
```

#### External Services

```
Paymob / Stripe Cloudinary QRCode Generator
```

## 8. Background Services

#### 8.1 Subscription Expiration Service

- Periodically checks for expired subscriptions
- Sends expiration reminder notifications
- Automatically disables expired subscriptions

#### 8.2 Booking Reminder Service

- Sends automated reminders before scheduled gym sessions
- Reduces no-show rates and improves attendance

## 9. Security

```
JWT Authentication Stateless token-based auth for all API requests
```

```
Role-Based Authorization Admin and GymMember roles with protected endpoints
```

```
Password Hashing Secure hashing via ASP.NET Identity (PBKDF2)
```

```
Protected APIs All sensitive routes require valid JWT tokens
```

```
Input Validation FluentValidation on all incoming requests
```

```
Secure Payment Handling Payment data handled via trusted gateways only
```

## 10. APIs Overview

#### Authentication

```
P
O
ST
```

```
/api/auth/register Register a new user
```

```
P
O
ST
```

```
/api/auth/login Login and receive JWT token
```

#### Subscription & Payment

```
G
ET /api/plans Get all subscription plans
```

```
G
ET /api/plans/{id} Get plan by ID
```

```
P
O
ST
```

```
/api/payments/create Initiate a payment
```

```
P
O
ST
```

```
/api/payments/confirm Confirm payment & activate subscription
```

#### Booking

```
P
O
ST
```

```
/api/bookings Create a new booking
```

```
G
ET /api/bookings Get user bookings
```

#### Attendance

```
P
O
ST
```

```
/api/attendance/scan Scan QR & record attendance
```

#### AI Chatbot

```
P
O
ST
```

```
/api/chat Send message to AI
```

```
G
ET /api/chat/history Get chat history
```

#### Workout & Nutrition

```
G
ET /api/workouts/current Get current workout plan
```

```
G
ET /api/nutrition/current Get current nutrition plan
```

#### Notifications

```
G
ET /api/notifications Get user notifications
```

## 11. AI Workflow

#### AI Input — Data the AI Uses

- User profile (name, weight, height, goal, activity level)
- Chat history for conversational context
- Subscription status
- Previous bookings and attendance history

#### AI Output — What the AI Produces

- Personalised workout plans
- Personalised nutrition plans
- Booking actions (create / reschedule / cancel)
- Smart gym schedule recommendations
- Fitness progress analysis
- Context-aware conversational responses

#### AI Request Lifecycle

##### 1 User sends message to AI chatbot

##### 2 Prompt generated with user context & history

##### 3 Request sent to OpenAI API

##### 4 AI response received

##### 5 Intent extracted (booking / workout / nutrition / general)

##### 6 Appropriate backend service selected

##### 7 Action executed through business service layer

##### 8 Entities created or updated in database

##### 9 Final response returned to user

#### Important Architecture Note

```
The AI does not access the database directly.
```

```
The AI's role is to understand intent and extract information. All database operations are performed
by the backend business service layer. The correct flow is: User → Controller → AI Service →
Business Services → Database.
```

## Conclusion

Arena combines modern gym management with the power of artificial intelligence to deliver a smart,
personalised fitness platform. The system provides end-to-end coverage from subscription and payment
handling to AI-driven workout planning, nutrition guidance, and conversational gym booking.

```
Intelligent Fitness
Guidance
```

```
AI-generated workout and nutrition plans tailored to each
member's profile and goals.
```

```
Automated Operations Booking, attendance, subscriptions, and reminders run with
minimal manual intervention.
```

```
Secure & Scalable Backend Onion Architecture with JWT auth, role-based access, and clean
service separation.
```

```
Modern Tech Stack ASP.NET Core API + Angular frontend + OpenAI + Paymob
payments.
```
