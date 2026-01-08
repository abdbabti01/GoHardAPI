using GoHardAPI.Data;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Register AI Services
builder.Services.AddScoped<GoHardAPI.Services.AI.AnthropicProvider>();
builder.Services.AddScoped<GoHardAPI.Services.AI.OpenAIProvider>();
builder.Services.AddScoped<GoHardAPI.Services.AI.AIProviderFactory>();
builder.Services.AddScoped<AIService>();

// Add HttpClient for AI providers (required by Anthropic SDK)
builder.Services.AddHttpClient();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
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
            Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations and seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TrainingContext>();

    try
    {
        // Create database schema if it doesn't exist
        // EnsureCreated() will NOT delete existing data - it only creates if missing
        var created = context.Database.EnsureCreated();

        if (created)
        {
            Console.WriteLine("New database created - seeding initial data...");
        }
        else
        {
            Console.WriteLine("Database already exists - checking for updates...");
        }

        // Always run seed data initialization
        // It will either insert new exercises or update existing ones with video URLs
        SeedData.Initialize(context);
        Console.WriteLine("Database seed/update completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Disabled for development - app uses HTTP
// app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Serve static files (profile photos)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
