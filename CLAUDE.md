# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GoHardAPI is an ASP.NET Core 8.0 Web API for managing training/workout sessions. It uses Entity Framework Core with SQL Server and follows a standard REST API architecture.

## Database Architecture

- **Database**: SQL Server (Local instance: `MSI\MSSQLSERVER01`)
- **Database Name**: `TrainingAppDb`
- **ORM**: Entity Framework Core 8.0
- **DbContext**: `TrainingContext` (GoHardAPI/Data/TrainingContext.cs)

### Data Model Relationships
The data model has full relationship support with cascade delete configured:

- **User** - Users of the training app
  - Properties: Id, Name, Email, DateCreated, Height, Weight, Goals
  - One-to-many with Sessions (cascade delete)
  - One-to-many with custom ExerciseTemplates (set null on delete)

- **Session** - Training sessions associated with users
  - Properties: Id, UserId, Date, Duration, Notes, Type
  - Many-to-one with User (cascade delete when user deleted)
  - One-to-many with Exercises (cascade delete)

- **Exercise** - Individual exercises performed in a session
  - Properties: Id, SessionId, Name, Sets, Reps, Weight, Duration, RestTime, Notes, ExerciseTemplateId
  - Many-to-one with Session (cascade delete when session deleted)
  - Many-to-one with ExerciseTemplate (optional, set null on delete)

- **ExerciseTemplate** - Library of exercise definitions (system and user-created)
  - Properties: Id, Name, Description, Category, MuscleGroup, Equipment, Difficulty, VideoUrl, ImageUrl, Instructions, IsCustom, CreatedByUserId
  - System templates (IsCustom=false) cannot be deleted
  - User templates (IsCustom=true) can be deleted
  - Many-to-one with User (optional, for custom templates)

**Important**:
- The connection string uses `TrustServerCertificate=True` for local SQL Server
- All models have validation attributes ([Required], [MaxLength], [EmailAddress])
- Database auto-seeds with 18 exercise templates on first run

## Build & Run Commands

### Build
```bash
dotnet build GoHardAPI.sln
# OR for Release configuration
dotnet build GoHardAPI.sln -c Release
```

### Run the API
```bash
cd GoHardAPI
dotnet run
```

### Restore NuGet packages
```bash
dotnet restore
```

## Database Migrations

When modifying models, create and apply migrations:

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project GoHardAPI

# Apply migrations to database
dotnet ef database update --project GoHardAPI

# Remove last migration (if not applied)
dotnet ef migrations remove --project GoHardAPI
```

## API Structure

### Controllers
All controllers follow RESTful conventions with standard CRUD operations:

- **UsersController** (`api/users`) - Manages users
  - GET, GET by id, POST, PUT, DELETE

- **SessionsController** (`api/sessions`) - Manages training sessions
  - GET, GET by id, POST, PUT, DELETE
  - Sessions automatically include their Exercises via `.Include(ts => ts.Exercises)`

- **ExerciseTemplatesController** (`api/exercisetemplates`) - Exercise library
  - GET (with filtering: category, muscleGroup, equipment, isCustom)
  - GET by id
  - GET `/categories` - returns distinct categories
  - GET `/musclegroups` - returns distinct muscle groups
  - POST, PUT, DELETE
  - System templates (IsCustom=false) cannot be deleted
  - Custom templates automatically marked with IsCustom=true if CreatedByUserId is set

- **WeatherForecastController** - Template controller (can be removed)

### Key Conventions
- All API controllers use `[Route("api/[controller]")]` attribute routing
- Entity Framework context is injected via constructor DI
- Controllers return `ActionResult<T>` for type-safe responses
- Sessions controller uses `.Include()` to eagerly load related Exercises

## Swagger/OpenAPI

Swagger UI is enabled in Development environment:
- Run the API and navigate to `/swagger` to view API documentation
- Configured via `AddSwaggerGen()` and `AddEndpointsApiExplorer()` in Program.cs:7-12

## Azure DevOps CI/CD

The project uses Azure Pipelines (azure-pipelines.yml):
- **Triggers**: `master` branch and `release/*` branches
- **Agent Pool**: `default` (custom agent pool)
- **Build Steps**: NuGet restore → MSBuild → VSTest → Publish artifacts
- **Artifact Name**: `buldArtifact` (note: contains typo in pipeline)

When modifying the pipeline, note it uses MSBuild (not `dotnet build`) with specific deployment parameters for IIS packaging.

## CORS Configuration

CORS is configured with "AllowAll" policy to support frontend applications:
- Allows all origins, methods, and headers
- For production, restrict to specific origins in Program.cs:11-20

## Seed Data

On application startup, the database is automatically seeded with 18 exercise templates:
- Chest: Bench Press, Push-ups, Dumbbell Flyes
- Back: Deadlift, Pull-ups, Bent-Over Row
- Legs: Squat, Lunges, Leg Press
- Shoulders: Overhead Press, Lateral Raises
- Arms: Bicep Curls, Tricep Dips
- Core: Plank, Crunches
- Cardio: Running, Jump Rope, Burpees

Seed logic in GoHardAPI/Data/SeedData.cs:8 - only runs if ExerciseTemplates table is empty.

## Development Notes

- Target Framework: .NET 8.0
- Nullable reference types enabled
- Implicit usings enabled
- The project uses minimal API setup pattern in Program.cs
- All models use data annotations for validation
- Entity relationships configured via Fluent API in TrainingContext.OnModelCreating
