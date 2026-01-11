# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GoHardAPI is an ASP.NET Core 8.0 Web API for a comprehensive fitness tracking application with offline-first mobile client support. The API supports workout session tracking, exercise management, AI chat integration, social features, body metrics, and structured workout programs.

## Database Architecture

### Connection Strings
- **Local Development**: SQL Server at `MSI\MSSQLSERVER01`, database `TrainingAppDb`
- **Production (Railway)**: PostgreSQL (auto-detected via `DATABASE_URL` environment variable)
- The API automatically switches database providers based on environment

### Core Entities & Relationships

```
User (1) ──→ (Many) Sessions
User (1) ──→ (Many) Goals
User (1) ──→ (Many) BodyMetrics
User (1) ──→ (Many) ChatConversations
User (1) ──→ (Many) ExerciseTemplates (custom only)

Session (1) ──→ (Many) Exercises
Session (1) ──→ (Many) ExerciseSets
Session (0,1) ──→ Program
Session (0,1) ──→ ProgramWorkout

Exercise (Many) ──→ (1) ExerciseTemplate (optional)

Goal (1) ──→ (Many) GoalProgress

ChatConversation (1) ──→ (Many) ChatMessages

SharedWorkout ──→ SharedWorkoutLikes, SharedWorkoutSaves

WorkoutTemplate (1) ──→ (Many) WorkoutTemplateRatings

Program (1) ──→ (Many) ProgramWorkouts
ProgramWorkout (1) ──→ (Many) Exercises
```

### Important Database Notes
- All DateTime fields use UTC for PostgreSQL compatibility
- Cascade delete relationships for data integrity
- User data isolation: All queries filter by current user from JWT
- Seed data: 18 exercise templates loaded on first run (GoHardAPI/Data/SeedData.cs)

## Build & Run Commands

### Standard Development
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build GoHardAPI.sln
dotnet build GoHardAPI.sln -c Release

# Run API (from GoHardAPI directory)
cd GoHardAPI
dotnet run
# Listens on http://0.0.0.0:5121
# Swagger UI: http://localhost:5121/swagger
```

### Database Migrations
```bash
# Create migration
dotnet ef migrations add <MigrationName> --project GoHardAPI

# Apply migrations
dotnet ef database update --project GoHardAPI

# Remove last migration (if not applied)
dotnet ef migrations remove --project GoHardAPI

# IMPORTANT: Program.cs automatically runs migrations on startup
# and includes cleanup logic for broken migrations
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage (if configured)
dotnet test /p:CollectCoverage=true
```

### Docker Deployment
```bash
# Build image
docker build -t gohard-api .

# Run container
docker run -p 8080:8080 -e PORT=8080 gohard-api
```

## API Structure

### Controllers (14+ endpoints)

All controllers use `[Authorize]` attribute for JWT protection and include `GetCurrentUserId()` helper to extract user from JWT claims.

| Controller | Endpoint | Key Features |
|-----------|----------|--------------|
| **AuthController** | `/api/auth` | Signup, Login (no auth required) |
| **UsersController** | `/api/users` | User CRUD, profile management |
| **SessionsController** | `/api/sessions` | Session CRUD, status updates, pause/resume, from-program-workout |
| **ExercisesController** | `/api/exercises` | Exercise CRUD within sessions |
| **ExerciseSetsController** | `/api/exercisesets` | Set tracking (reps, weight, completion) |
| **ExerciseTemplatesController** | `/api/exercisetemplates` | Browse templates, filter by category/muscle/equipment, `/categories`, `/musclegroups` |
| **AnalyticsController** | `/api/analytics` | Volume trends, streaks, personal records |
| **GoalsController** | `/api/goals` | Goals CRUD, progress tracking |
| **BodyMetricsController** | `/api/bodymetrics` | Weight, body fat, BMI tracking |
| **ChatController** | `/api/chat` | AI conversation management (Anthropic, OpenAI, Groq, Gemini) |
| **ProfileController** | `/api/profile` | Profile updates, photo upload |
| **ProgramsController** | `/api/programs` | Workout programs and program workouts |
| **WorkoutTemplatesController** | `/api/workouttemplates` | Recurring workout schedules |
| **SharedWorkoutsController** | `/api/sharedworkouts` | Community workouts, likes, saves, ratings |

### Authentication & Security

**JWT Configuration** (appsettings.json):
- Algorithm: HMAC SHA256
- Issuer: "GoHardAPI"
- Audience: "GoHardApp"
- Expiration: 720 hours (30 days)
- Claims: NameIdentifier (user ID), Name, Email, Jti

**Password Security**:
- BCrypt.Net-Next for hashing
- Verification uses constant-time comparison

**Getting a Token**:
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}

Response: { "token": "...", "userId": 1, "name": "...", "email": "..." }
```

**Using Token**:
```http
Authorization: Bearer <token>
```

### AI Integration

The API supports multiple AI providers configured in appsettings.json:
- **Anthropic**: claude-3-haiku-20240307
- **OpenAI**: gpt-4
- **Groq**: llama-3.3-70b-versatile (default)
- **Gemini**: gemini-pro

**Services**:
- `AIService`: Manages conversations with streaming support
- `AIProviderFactory`: Provider selection and instantiation
- System prompts customized for fitness coaching

## Development Workflow

### Adding a New Endpoint

1. **Create/Update Model** (GoHardAPI/Models/YourModel.cs):
```csharp
public class YourModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

2. **Add to DbContext** (GoHardAPI/Data/TrainingContext.cs):
```csharp
public DbSet<YourModel> YourModels { get; set; }
```

3. **Configure Relationships** in `OnModelCreating`:
```csharp
modelBuilder.Entity<YourModel>()
    .HasOne(y => y.User)
    .WithMany()
    .HasForeignKey(y => y.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

4. **Create Migration**:
```bash
dotnet ef migrations add AddYourModel --project GoHardAPI
dotnet ef database update --project GoHardAPI
```

5. **Create Controller** (GoHardAPI/Controllers/YourModelController.cs):
```csharp
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class YourModelController : ControllerBase
{
    private readonly TrainingContext _context;

    public YourModelController(TrainingContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<YourModel>>> GetAll()
    {
        var userId = GetCurrentUserId();
        return await _context.YourModels
            .Where(y => y.UserId == userId)
            .ToListAsync();
    }
}
```

### Common Patterns

**User Data Isolation**:
Always filter queries by current user to prevent data leakage:
```csharp
var userId = GetCurrentUserId();
var items = await _context.Items
    .Where(i => i.UserId == userId)
    .ToListAsync();
```

**Eager Loading**:
Use `.Include()` for related data:
```csharp
var session = await _context.Sessions
    .Include(s => s.Exercises)
    .ThenInclude(e => e.ExerciseSets)
    .FirstOrDefaultAsync(s => s.Id == id);
```

**UTC DateTime**:
Always use UTC for timestamps:
```csharp
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

## Configuration Files

### appsettings.json

Key sections:
- **ConnectionStrings**: Database connection (overridden by DATABASE_URL in production)
- **JwtSettings**: Token configuration
- **AISettings**: Multi-provider AI configuration with API keys

### Program.cs

Application setup includes:
1. Database provider selection (PostgreSQL vs SQL Server)
2. Service registration (Auth, AI, File Upload)
3. JWT authentication configuration
4. CORS setup ("AllowAll" policy)
5. Entity Framework Core
6. Swagger/OpenAPI
7. Automatic migrations and seed data
8. Static file serving for profile photos

## CI/CD Pipeline

**Azure Pipelines** (azure-pipelines.yml):
- Triggers: `master` and `release/*` branches
- Agent Pool: `default`
- Steps: NuGet restore → MSBuild → VSTest → Publish artifacts
- Artifact name: `buldArtifact`

**Manual Deployment**:
```bash
dotnet publish -c Release -o ./publish
# Deploy to IIS, Azure App Service, or Railway
```

## Troubleshooting

### Database Connection Issues

**SQL Server**:
```bash
# Verify server is running
sqlcmd -S MSI\MSSQLSERVER01 -Q "SELECT @@VERSION"

# Check connection string in appsettings.json
# Must include TrustServerCertificate=True for local dev
```

**PostgreSQL (Production)**:
- Connection string parsed from DATABASE_URL environment variable
- Format: `postgresql://user:password@host:port/database`

### Migration Issues

**Broken Migration**:
Program.cs includes automatic cleanup logic that detects and removes problematic migrations on startup.

**Manual Cleanup**:
```bash
# Remove last migration
dotnet ef migrations remove --project GoHardAPI

# Recreate it
dotnet ef migrations add <MigrationName> --project GoHardAPI
dotnet ef database update --project GoHardAPI
```

### Testing with Swagger

Navigate to `/swagger` when API is running to:
- Test all endpoints interactively
- View request/response schemas
- Authenticate with JWT tokens (click "Authorize" button)

## Architecture Notes

**Entry Point**: GoHardAPI/Program.cs
- Minimal API hosting pattern
- Dependency injection configured
- Middleware pipeline setup

**DbContext**: GoHardAPI/Data/TrainingContext.cs
- All entity configurations
- Fluent API for relationships
- Seed data logic

**Services**:
- `AuthService`: JWT generation, password hashing/verification
- `AIService`: Multi-provider AI chat integration
- `FileUploadService`: Profile photo uploads

**Models**: GoHardAPI/Models/
- 17+ data models with validation attributes
- Navigation properties for relationships
- UTC DateTime for all timestamps
