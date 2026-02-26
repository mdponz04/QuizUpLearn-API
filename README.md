# QuizUpLearn API

A full-featured RESTful API for an English-language quiz and learning platform built with ASP.NET Core 8.0 and PostgreSQL.

## Table of Contents

- [Frontend](#frontend)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
- [Installation](#installation)
- [Environment Variables](#environment-variables)
- [Running the App](#running-the-app)
- [API Documentation](#api-documentation)

## Frontend

- **Github**: [Link](https://github.com/Hans2374/QuizUpLearn-Web)

## Features

- **User Management** - Registration, login, profile management
- **Authentication** - JWT-based authentication with refresh tokens
- **Google OAuth Integration** - Login with Google
- **Role-Based Access Control** - Admin and user roles
- **Quiz Management** - CRUD operations for quizzes, quiz sets, and answer options
- **Real-Time Multiplayer** - SignalR-powered multiplayer quiz games and 1v1 battles
- **Tournaments & Events** - Competitive tournaments and scheduled events with background schedulers
- **Placement Tests** - TOEIC-style placement tests (Part 1–7) with level assessment
- **Grammar & Vocabulary** - Structured grammar and vocabulary learning content
- **AI-Powered Features** - Google Gemini, OpenRouter, and Nebius LLM integrations
- **Text-to-Speech** - AsyncTTS integration with multiple voice options
- **Subscription & Payment** - Subscription plans with PayOS payment gateway integration
- **Badge & Achievement System** - Progress, skill, consistency, and improvement badges
- **Mistake Tracking** - User mistake review and weak point analysis
- **Push Notifications** - Firebase Cloud Messaging for real-time notifications
- **File Upload** - Cloudinary integration for image uploads
- **Email Service** - OTP verification and transactional emails via MailerSend
- **Excel Export** - Data export functionality with EPPlus
- **API Documentation** - Swagger UI documentation

## Tech Stack

- **Framework**: [ASP.NET Core 8.0](https://dotnet.microsoft.com/en-us/apps/aspnet) (.NET 8)
- **Language**: [C#](https://learn.microsoft.com/en-us/dotnet/csharp/)
- **Database**: [PostgreSQL](https://www.postgresql.org/) with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- **Caching**: [Redis](https://redis.io/) (StackExchange.Redis)
- **Real-Time**: [SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/) with Redis backplane
- **Authentication**: JWT Bearer, Google OAuth, Firebase Admin SDK
- **AI / LLM**: Google Gemini, OpenRouter, Nebius
- **File Storage**: Cloudinary
- **Email**: MailerSend
- **Payment**: PayOS
- **Push Notifications**: Firebase Cloud Messaging
- **Mapping**: AutoMapper
- **Excel Export**: EPPlus
- **Documentation**: Swagger (Swashbuckle)
- **Testing**: xUnit, Moq, FluentAssertions
- **Containerization**: Docker

## Project Structure

The solution is organized into the following projects:

```
QuizUpLearn/
├── QuizUpLearn.API/        # ASP.NET Core Web API (controllers, hubs, middleware, DI)
│   ├── Controllers/        # 40 API controllers
│   ├── Hubs/               # SignalR hubs (GameHub, OneVsOneHub, BackgroundJobHub)
│   ├── Middlewares/         # Exception handling & API response wrapping
│   └── DI/                 # Dependency injection configuration
├── BusinessLogic/          # Service layer, DTOs, interfaces, AutoMapper profiles
│   ├── Services/           # 44 service implementations
│   ├── Interfaces/         # Service contracts
│   ├── DTOs/               # Request/response DTOs (28 feature subdirectories)
│   └── MappingProfile/     # AutoMapper profiles
├── Repository/             # Data access layer
│   ├── Entities/           # 35 entity models
│   ├── DBContext/           # EF Core DbContext (PostgreSQL)
│   ├── Repositories/       # Repository implementations
│   ├── Enums/              # Domain enumerations
│   └── Migrations/         # EF Core migrations
└── QuizUpLearn.Test/       # Unit tests (xUnit + Moq + FluentAssertions)
```

## API Endpoints

The API includes the following main endpoints:

- `/api/auth` - Authentication (login, register, JWT, Google OAuth, refresh tokens)
- `/api/users` - User profile management
- `/api/accounts` - Account management
- `/api/roles` - Role management
- `/api/quizsets` - Quiz set CRUD
- `/api/quizzes` - Quiz CRUD
- `/api/answeroptions` - Answer option management
- `/api/quizattempts` - Quiz attempt tracking
- `/api/quizattemptdetails` - Per-question attempt details
- `/api/placementquizsets` - Placement / level tests
- `/api/games` - Real-time multiplayer game lobby
- `/api/onevsone` - 1v1 quiz battles
- `/api/tournaments` - Tournament management
- `/api/events` - Event management
- `/api/grammars` - Grammar content
- `/api/vocabularies` - Vocabulary content
- `/api/vocabularygrammar` - Combined vocabulary & grammar
- `/api/ai` - AI-powered features (Gemini, OpenRouter, Nebius)
- `/api/subscriptions` - Subscription management
- `/api/subscriptionplans` - Subscription plan CRUD
- `/api/subscriptionusage` - Usage tracking
- `/api/payments` - PayOS payment processing
- `/api/paymenttransactions` - Transaction records
- `/api/buysubscriptions` - Subscription purchase flow
- `/api/badges` - Achievement badges
- `/api/usermistakes` - Mistake tracking for review
- `/api/userweakpoints` - Weak area analysis
- `/api/notifications` - Notification templates
- `/api/usernotifications` - User notification delivery
- `/api/quizsetcomments` - Comments on quiz sets
- `/api/userquizsetfavorites` - Favorite quiz sets
- `/api/userquizsetlikes` - Like/unlike quiz sets
- `/api/quizreports` - Quiz content reports
- `/api/userreports` - User behavior reports
- `/api/uploads` - Cloudinary file uploads
- `/api/mails` - Email / OTP services
- `/api/dashboards` - User dashboard & stats
- `/api/admindashboard` - Admin analytics dashboard
- `/api/quizgroupitems` - Quiz grouping
- `/api/quizquizsets` - Quiz-to-QuizSet mappings

### SignalR Hubs

- `/game-hub` - Real-time multiplayer quiz game
- `/one-vs-one-hub` - 1v1 quiz battles
- `/background-jobs` - Background job status push

## Authentication

The API supports multiple authentication strategies:

- **Local Authentication**: Username/password login with BCrypt hashing
- **JWT Authentication**: Token-based authentication (120 min access / 7 day refresh)
- **Google OAuth**: Social login via Google APIs
- **Firebase Authentication**: Firebase Admin SDK integration
- **Refresh Token**: Token renewal

Access control is implemented using JWT Bearer middleware and custom authorization attributes.

## Installation

```bash
# Clone the repository
git clone <repository-url>

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project Repository --startup-project QuizUpLearn.API
```

## Environment Variables

Configure the following sections in `appsettings.json` or via environment variables:

```
# PostgreSQL
ConnectionStrings__PostgreSqlConnection=Host=your-host;Database=your-db;Username=your-user;Password=your-password

# JWT
Jwt__Key=your-jwt-secret-key
Jwt__Issuer=QuizUpLearn
Jwt__Audience=QuizUpLearnClient
Jwt__AccessTokenExpirationMinutes=120
Jwt__RefreshTokenExpirationDays=7

# Cloudinary
Cloudinary__CloudName=your-cloud-name
Cloudinary__APIKey=your-api-key
Cloudinary__APISecret=your-api-secret

# MailerSend
MailerSend__ApiKey=your-mailersend-api-key

# Gemini AI
Gemini__ApiKey=your-gemini-api-key

# OpenRouter
OpenRouter__ApiKey=your-openrouter-api-key

# Nebius AI
Nebius__ApiKey=your-nebius-api-key

# AsyncTTS (Text-to-Speech)
AsyncTTS__ApiUrl=your-tts-api-url
AsyncTTS__MaleVoiceId=your-male-voice-id
AsyncTTS__FemaleVoiceId=your-female-voice-id
AsyncTTS__NarratorVoiceId=your-narrator-voice-id

# Redis
Redis__ConnectionString=your-redis-connection-string

# PayOS
PayOS__ClientId=your-payos-client-id
PayOS__ApiKey=your-payos-api-key
PayOS__ChecksumKey=your-payos-checksum-key

# Firebase
Firebase__ServiceAccountJson=your-firebase-service-account-json
```

## Running the App

```bash
# Development mode
dotnet run --project QuizUpLearn.API

# Production mode
dotnet run --project QuizUpLearn.API --configuration Release

# Docker
docker-compose up
```

The API will be available at `http://localhost:5005`.

## API Documentation

Swagger UI documentation is available at `http://localhost:5005/swagger` when the application is running.

## Features in Detail

### Quiz & Learning

- TOEIC-style quizzes with 7 parts (Part 1–Part 7)
- Quiz sets for practice, placement, tournaments, events, and weak point fixing
- Grammar and vocabulary lessons with difficulty levels (easy, medium, hard)
- Mistake tracking and personalized weak point analysis
- AI-powered quiz generation and learning assistance

### Real-Time Multiplayer

- SignalR-powered multiplayer quiz games with lobby system
- 1v1 real-time quiz battles
- Live leaderboards and result screens
- Redis-backed game state management for scalability

### Tournaments & Events

- Tournament creation and management with quiz set assignments
- Event scheduling with background task automation
- Participant tracking and ranking

### Subscription & Payment

- Subscription plans with usage metering
- PayOS payment gateway integration
- Transaction history and status tracking

### Badge & Achievement System

- Progress, skill, consistency, improvement, and special badge types
- Configurable badge rules (quiz count, score percentage, streak days, etc.)
- Automatic badge awarding

### Notifications

- Firebase Cloud Messaging push notifications
- System, quiz, event, tournament, achievement, subscription, social, reminder, security, and marketing notification types

## 👥 Team

-   Backend Developers: [trikmgithub]
-   Database Developers: [trikmgithub]

## 📝 Backend

-   Docker: `mdpz04/qul-api:latest`
-   Local: http://localhost:5005/

## 🙏 Acknowledgments

-   ASP.NET Core framework
-   Entity Framework Core
-   SignalR for real-time communication