using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Repositories;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Support both SQL Server (local) and PostgreSQL (Railway production)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

// Debug logging
Console.WriteLine($"DATABASE_URL exists: {databaseUrl != null}");
if (databaseUrl != null)
{
    Console.WriteLine($"DATABASE_URL value: {databaseUrl.Substring(0, Math.Min(20, databaseUrl.Length))}...");
}

// Use PostgreSQL if DATABASE_URL is set (Railway), otherwise use SQL Server (local)
if (databaseUrl != null)
{
    // Railway PostgreSQL URL format: postgresql://user:password@host:port/database
    // Convert to Npgsql format
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

    builder.Services.AddDbContext<TrainingContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddDbContext<TrainingContext>(options =>
        options.UseSqlServer(connectionString));
}

// Register AuthService
builder.Services.AddScoped<AuthService>();

// Register FileUploadService
builder.Services.AddScoped<FileUploadService>();

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExerciseTemplateRepository, ExerciseTemplateRepository>();

// Register background services
builder.Services.AddHostedService<DraftSessionCleanupService>();

// Register AI Services
builder.Services.AddScoped<GoHardAPI.Services.AI.AnthropicProvider>();
builder.Services.AddScoped<GoHardAPI.Services.AI.OpenAIProvider>();
builder.Services.AddScoped<GoHardAPI.Services.AI.GroqProvider>();
builder.Services.AddScoped<GoHardAPI.Services.AI.AIProviderFactory>();
builder.Services.AddScoped<AIService>();

// Add HttpClient for AI providers (required by Anthropic SDK)
builder.Services.AddHttpClient();

// Register OpenFoodFacts Service
builder.Services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "GoHardAPI/1.0");
});

// Configure JWT Authentication
// SECURITY: JWT secret MUST be set via environment variable in production
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? jwtSettings["Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT_SECRET environment variable is required. " +
        "Set it in your environment or Railway dashboard.");
}

if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET must be at least 32 characters long for security.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret))
    };
});

// Add CORS - SECURITY: Restrict to known origins in production
var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5121" };

// Also allow origins from environment variable (comma-separated)
var envOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
if (!string.IsNullOrEmpty(envOrigins))
{
    corsOrigins = corsOrigins.Concat(envOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToArray();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfigured",
        policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });

    // Separate policy for mobile apps (allow any origin but no credentials)
    options.AddPolicy("AllowMobileApps",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Add Rate Limiting - SECURITY: Prevent brute force attacks on auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict rate limit for authentication endpoints (login/signup)
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;  // 5 attempts
        limiterOptions.Window = TimeSpan.FromMinutes(1);  // per minute
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    // General API rate limit (more permissive)
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;  // 100 requests
        limiterOptions.Window = TimeSpan.FromMinutes(1);  // per minute
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    // Per-IP rate limiting for unauthenticated requests
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "GoHard API",
        Version = "v1",
        Description = "Fitness tracking API for workout management, analytics, and AI coaching"
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TrainingContext>("database");

var app = builder.Build();

// Apply migrations and seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TrainingContext>();

    try
    {
        // Railway database was created with EnsureCreated (no migration history)
        // We need to manually create migration history for existing tables
        // Then use Migrate() to apply only new migrations (Programs)

        Console.WriteLine("Checking migration history...");

        // Clean up old broken migration entries that no longer exist in codebase
        try
        {
            var result = context.Database.ExecuteSqlRaw(
                "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '20260109202221_%'");
            if (result > 0)
            {
                Console.WriteLine($"Cleaned up {result} old migration entry/entries");
            }
        }
        catch { /* Ignore if table doesn't exist */ }

        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        var appliedMigrations = context.Database.GetAppliedMigrations().ToList();

        Console.WriteLine($"Applied migrations: {appliedMigrations.Count}");
        Console.WriteLine($"Pending migrations: {pendingMigrations.Count}");

        if (appliedMigrations.Count == 0 && pendingMigrations.Count > 0)
        {
            Console.WriteLine("No migration history found, but migrations exist.");
            Console.WriteLine("Manually marking old migrations as applied...");

            // Get all migrations except any Programs-related ones
            var allMigrations = pendingMigrations.Where(m =>
                !m.Contains("AddProgram") &&
                !m.StartsWith("202601092")).ToList();

            // Manually insert into __EFMigrationsHistory
            foreach (var migration in allMigrations)
            {
                try
                {
                    context.Database.ExecuteSqlRaw(
                        "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                        migration, "8.0.10");
                    Console.WriteLine($"Marked as applied: {migration}");
                }
                catch
                {
                    // Migration history might already exist, ignore
                }
            }
        }

        // List all migrations after cleanup
        var allMigrationsFinal = context.Database.GetMigrations().ToList();
        var appliedMigrationsFinal = context.Database.GetAppliedMigrations().ToList();
        var pendingMigrationsFinal = context.Database.GetPendingMigrations().ToList();

        Console.WriteLine($"\n📋 Migration Status:");
        Console.WriteLine($"Total migrations: {allMigrationsFinal.Count}");
        Console.WriteLine($"Applied migrations: {appliedMigrationsFinal.Count}");
        Console.WriteLine($"Pending migrations: {pendingMigrationsFinal.Count}");

        if (pendingMigrationsFinal.Any())
        {
            Console.WriteLine("\n⏳ Pending migrations to apply:");
            foreach (var migration in pendingMigrationsFinal)
            {
                Console.WriteLine($"  - {migration}");
            }
        }

        // Handle AddNutritionTracking migration specially for PostgreSQL compatibility
        if (pendingMigrationsFinal.Any(m => m.Contains("AddNutritionTracking")))
        {
            Console.WriteLine("\n🔧 Applying AddNutritionTracking migration with PostgreSQL-compatible SQL...");
            try
            {
                // Create tables with PostgreSQL-compatible types
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""FoodTemplates"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(200) NOT NULL,
                        ""Brand"" VARCHAR(100),
                        ""Category"" VARCHAR(50),
                        ""Barcode"" VARCHAR(100),
                        ""ServingSize"" DECIMAL(18,2) NOT NULL,
                        ""ServingUnit"" VARCHAR(20) NOT NULL,
                        ""Calories"" DECIMAL(18,2) NOT NULL,
                        ""Protein"" DECIMAL(18,2) NOT NULL,
                        ""Carbohydrates"" DECIMAL(18,2) NOT NULL,
                        ""Fat"" DECIMAL(18,2) NOT NULL,
                        ""Fiber"" DECIMAL(18,2),
                        ""Sugar"" DECIMAL(18,2),
                        ""SaturatedFat"" DECIMAL(18,2),
                        ""TransFat"" DECIMAL(18,2),
                        ""Sodium"" DECIMAL(18,2),
                        ""Potassium"" DECIMAL(18,2),
                        ""Cholesterol"" DECIMAL(18,2),
                        ""VitaminA"" DECIMAL(18,2),
                        ""VitaminC"" DECIMAL(18,2),
                        ""VitaminD"" DECIMAL(18,2),
                        ""Calcium"" DECIMAL(18,2),
                        ""Iron"" DECIMAL(18,2),
                        ""Description"" VARCHAR(2000),
                        ""ImageUrl"" TEXT,
                        ""IsCustom"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedByUserId"" INTEGER REFERENCES ""Users""(""Id"") ON DELETE SET NULL,
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_FoodTemplates_Name"" ON ""FoodTemplates""(""Name"");
                    CREATE INDEX IF NOT EXISTS ""IX_FoodTemplates_Barcode"" ON ""FoodTemplates""(""Barcode"");
                    CREATE INDEX IF NOT EXISTS ""IX_FoodTemplates_Category_IsCustom"" ON ""FoodTemplates""(""Category"", ""IsCustom"");
                    CREATE INDEX IF NOT EXISTS ""IX_FoodTemplates_CreatedByUserId"" ON ""FoodTemplates""(""CreatedByUserId"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MealLogs"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INTEGER NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                        ""Date"" TIMESTAMP NOT NULL,
                        ""Notes"" VARCHAR(1000),
                        ""WaterIntake"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalCalories"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalProtein"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalCarbohydrates"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalFat"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalFiber"" DECIMAL(18,2),
                        ""TotalSodium"" DECIMAL(18,2),
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MealLogs_UserId_Date"" ON ""MealLogs""(""UserId"", ""Date"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MealPlans"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INTEGER NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""Description"" VARCHAR(1000),
                        ""DurationDays"" INTEGER NOT NULL DEFAULT 7,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""AverageDailyCalories"" DECIMAL(18,2),
                        ""AverageDailyProtein"" DECIMAL(18,2),
                        ""AverageDailyCarbohydrates"" DECIMAL(18,2),
                        ""AverageDailyFat"" DECIMAL(18,2),
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_MealPlans_UserId_IsActive"" ON ""MealPlans""(""UserId"", ""IsActive"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""NutritionGoals"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INTEGER NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
                        ""Name"" VARCHAR(100),
                        ""DailyCalories"" DECIMAL(18,2) NOT NULL,
                        ""DailyProtein"" DECIMAL(18,2) NOT NULL,
                        ""DailyCarbohydrates"" DECIMAL(18,2) NOT NULL,
                        ""DailyFat"" DECIMAL(18,2) NOT NULL,
                        ""DailyFiber"" DECIMAL(18,2),
                        ""DailySodium"" DECIMAL(18,2),
                        ""DailySugar"" DECIMAL(18,2),
                        ""DailyWater"" DECIMAL(18,2),
                        ""ProteinPercentage"" DECIMAL(18,2),
                        ""CarbohydratesPercentage"" DECIMAL(18,2),
                        ""FatPercentage"" DECIMAL(18,2),
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_NutritionGoals_UserId_IsActive"" ON ""NutritionGoals""(""UserId"", ""IsActive"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MealEntries"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""MealLogId"" INTEGER NOT NULL REFERENCES ""MealLogs""(""Id"") ON DELETE CASCADE,
                        ""MealType"" VARCHAR(50) NOT NULL,
                        ""Name"" VARCHAR(100),
                        ""ScheduledTime"" TIMESTAMP,
                        ""IsConsumed"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""ConsumedAt"" TIMESTAMP,
                        ""Notes"" VARCHAR(500),
                        ""TotalCalories"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalProtein"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalCarbohydrates"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalFat"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalFiber"" DECIMAL(18,2),
                        ""TotalSodium"" DECIMAL(18,2),
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_MealEntries_MealLogId_MealType"" ON ""MealEntries""(""MealLogId"", ""MealType"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MealPlanDays"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""MealPlanId"" INTEGER NOT NULL REFERENCES ""MealPlans""(""Id"") ON DELETE CASCADE,
                        ""DayNumber"" INTEGER NOT NULL,
                        ""Name"" VARCHAR(50),
                        ""Notes"" VARCHAR(500),
                        ""TotalCalories"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalProtein"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalCarbohydrates"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalFat"" DECIMAL(18,2) NOT NULL DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_MealPlanDays_MealPlanId_DayNumber"" ON ""MealPlanDays""(""MealPlanId"", ""DayNumber"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""FoodItems"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""MealEntryId"" INTEGER NOT NULL REFERENCES ""MealEntries""(""Id"") ON DELETE CASCADE,
                        ""FoodTemplateId"" INTEGER REFERENCES ""FoodTemplates""(""Id"") ON DELETE SET NULL,
                        ""Name"" VARCHAR(200) NOT NULL,
                        ""Brand"" VARCHAR(100),
                        ""Quantity"" DECIMAL(18,2) NOT NULL DEFAULT 1,
                        ""ServingSize"" DECIMAL(18,2) NOT NULL,
                        ""ServingUnit"" VARCHAR(20) NOT NULL,
                        ""Calories"" DECIMAL(18,2) NOT NULL,
                        ""Protein"" DECIMAL(18,2) NOT NULL,
                        ""Carbohydrates"" DECIMAL(18,2) NOT NULL,
                        ""Fat"" DECIMAL(18,2) NOT NULL,
                        ""Fiber"" DECIMAL(18,2),
                        ""Sugar"" DECIMAL(18,2),
                        ""Sodium"" DECIMAL(18,2),
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_FoodItems_MealEntryId"" ON ""FoodItems""(""MealEntryId"");
                    CREATE INDEX IF NOT EXISTS ""IX_FoodItems_FoodTemplateId"" ON ""FoodItems""(""FoodTemplateId"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MealPlanMeals"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""MealPlanDayId"" INTEGER NOT NULL REFERENCES ""MealPlanDays""(""Id"") ON DELETE CASCADE,
                        ""MealType"" VARCHAR(50) NOT NULL,
                        ""Name"" VARCHAR(100),
                        ""ScheduledTime"" TIME,
                        ""Notes"" VARCHAR(500),
                        ""TotalCalories"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalProtein"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalCarbohydrates"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""TotalFat"" DECIMAL(18,2) NOT NULL DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_MealPlanMeals_MealPlanDayId_MealType"" ON ""MealPlanMeals""(""MealPlanDayId"", ""MealType"");
                ");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""MealPlanFoodItems"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""MealPlanMealId"" INTEGER NOT NULL REFERENCES ""MealPlanMeals""(""Id"") ON DELETE CASCADE,
                        ""FoodTemplateId"" INTEGER REFERENCES ""FoodTemplates""(""Id"") ON DELETE SET NULL,
                        ""Name"" VARCHAR(200) NOT NULL,
                        ""Quantity"" DECIMAL(18,2) NOT NULL DEFAULT 1,
                        ""ServingSize"" DECIMAL(18,2) NOT NULL,
                        ""ServingUnit"" VARCHAR(20) NOT NULL,
                        ""Calories"" DECIMAL(18,2) NOT NULL,
                        ""Protein"" DECIMAL(18,2) NOT NULL,
                        ""Carbohydrates"" DECIMAL(18,2) NOT NULL,
                        ""Fat"" DECIMAL(18,2) NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_MealPlanFoodItems_MealPlanMealId"" ON ""MealPlanFoodItems""(""MealPlanMealId"");
                    CREATE INDEX IF NOT EXISTS ""IX_MealPlanFoodItems_FoodTemplateId"" ON ""MealPlanFoodItems""(""FoodTemplateId"");
                ");

                // Mark the migration as applied
                var nutritionMigration = pendingMigrationsFinal.First(m => m.Contains("AddNutritionTracking"));
                context.Database.ExecuteSqlRaw(
                    @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ({0}, {1})",
                    nutritionMigration, "8.0.10");
                Console.WriteLine($"  ✓ Created nutrition tables and marked {nutritionMigration} as applied");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠️ Nutrition tables may already exist: {ex.Message}");
                // Try to mark migration as applied anyway
                try
                {
                    var nutritionMigration = pendingMigrationsFinal.First(m => m.Contains("AddNutritionTracking"));
                    context.Database.ExecuteSqlRaw(
                        @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ({0}, {1})",
                        nutritionMigration, "8.0.10");
                }
                catch { /* Ignore if already marked */ }
            }
        }

        // Now apply any remaining migrations
        Console.WriteLine("\n🔄 Applying pending migrations...");
        context.Database.Migrate();
        Console.WriteLine("✅ Migrations applied successfully!");

        // Fix cascade delete constraints (in case migrations didn't apply them)
        Console.WriteLine("\n🔧 Verifying and fixing cascade delete constraints...");
        try
        {
            // Fix Session -> Program cascade delete
            context.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Sessions""
                DROP CONSTRAINT IF EXISTS ""FK_Sessions_Programs_ProgramId"";

                ALTER TABLE ""Sessions""
                ADD CONSTRAINT ""FK_Sessions_Programs_ProgramId""
                FOREIGN KEY (""ProgramId"")
                REFERENCES ""Programs""(""Id"")
                ON DELETE CASCADE;
            ");
            Console.WriteLine("  ✓ Session -> Program cascade delete configured");

            // Fix Session -> ProgramWorkout cascade delete
            context.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Sessions""
                DROP CONSTRAINT IF EXISTS ""FK_Sessions_ProgramWorkouts_ProgramWorkoutId"";

                ALTER TABLE ""Sessions""
                ADD CONSTRAINT ""FK_Sessions_ProgramWorkouts_ProgramWorkoutId""
                FOREIGN KEY (""ProgramWorkoutId"")
                REFERENCES ""ProgramWorkouts""(""Id"")
                ON DELETE CASCADE;
            ");
            Console.WriteLine("  ✓ Session -> ProgramWorkout cascade delete configured");

            // Fix Program -> Goal cascade delete
            context.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Programs""
                DROP CONSTRAINT IF EXISTS ""FK_Programs_Goals_GoalId"";

                ALTER TABLE ""Programs""
                ADD CONSTRAINT ""FK_Programs_Goals_GoalId""
                FOREIGN KEY (""GoalId"")
                REFERENCES ""Goals""(""Id"")
                ON DELETE CASCADE;
            ");
            Console.WriteLine("  ✓ Program -> Goal cascade delete configured");

            // Fix ProgramWorkout -> Program cascade delete
            context.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""ProgramWorkouts""
                DROP CONSTRAINT IF EXISTS ""FK_ProgramWorkouts_Programs_ProgramId"";

                ALTER TABLE ""ProgramWorkouts""
                ADD CONSTRAINT ""FK_ProgramWorkouts_Programs_ProgramId""
                FOREIGN KEY (""ProgramId"")
                REFERENCES ""Programs""(""Id"")
                ON DELETE CASCADE;
            ");
            Console.WriteLine("  ✓ ProgramWorkout -> Program cascade delete configured");

            Console.WriteLine("✅ All cascade delete constraints verified and fixed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Warning: Could not update cascade delete constraints: {ex.Message}");
            // Don't throw - the app can still run, just cascade delete might not work
        }

        // Fix existing programs to start on Monday (next Monday from their original start date)
        Console.WriteLine("\n🔧 Checking program start date alignment...");
        try
        {
            var allPrograms = context.Programs.ToList();
            var misalignedPrograms = allPrograms
                .Where(p => p.StartDate.DayOfWeek != DayOfWeek.Monday)
                .ToList();

            if (misalignedPrograms.Any())
            {
                Console.WriteLine($"  Found {misalignedPrograms.Count} program(s) not starting on Monday");
                foreach (var program in misalignedPrograms)
                {
                    var oldDate = program.StartDate;
                    // Calculate days until next Monday (0 if already Monday)
                    var daysUntilMonday = ((int)DayOfWeek.Monday - (int)program.StartDate.DayOfWeek + 7) % 7;
                    if (daysUntilMonday == 0) daysUntilMonday = 7; // If somehow Monday, skip to next (shouldn't happen)
                    program.StartDate = program.StartDate.Date.AddDays(daysUntilMonday);

                    // Update end date to maintain same duration
                    if (program.EndDate != null)
                    {
                        program.EndDate = program.StartDate.AddDays(program.TotalWeeks * 7);
                    }

                    Console.WriteLine($"  ✓ Fixed \"{program.Title}\": {oldDate:yyyy-MM-dd} ({oldDate.DayOfWeek}) → {program.StartDate:yyyy-MM-dd} (Monday)");
                }
                context.SaveChanges();
                Console.WriteLine($"✅ Fixed {misalignedPrograms.Count} program(s) to start on Monday");
            }
            else
            {
                Console.WriteLine("  ✓ All programs already start on Monday");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Warning: Could not fix program dates: {ex.Message}");
        }

        // Always run seed data initialization
        SeedData.Initialize(context);
        FoodSeedData.Initialize(context);
        Console.WriteLine("Database seed/update completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
        throw;
    }
}

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI();

// Disabled for development - app uses HTTP
// app.UseHttpsRedirection();

// Use rate limiting before other middleware
app.UseRateLimiter();

// Use CORS - mobile apps use AllowMobileApps, web uses AllowConfigured
app.UseCors("AllowMobileApps");

// Serve static files (profile photos)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoint for monitoring/load balancers
app.MapHealthChecks("/health");

app.Run();
