using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// Boots the real <c>Program.cs</c> pipeline (environment <c>"Testing"</c>, so the
    /// startup DB-bootstrap block is skipped) with an isolated in-memory
    /// <see cref="TrainingContext"/> and a known JWT signing key, so the
    /// <c>session-write</c> rate-limiter policy and the middleware order can be exercised
    /// end to end over HTTP.
    ///
    /// Each instance owns a distinct in-memory database, its own limiter partitions, and
    /// its own create-call counter — tests cannot influence one another. Put limiter
    /// overrides in <see cref="ConfigOverrides"/> before creating the first client.
    /// </summary>
    public class SessionWriteRateLimitFactory : WebApplicationFactory<Program>
    {
        public const string JwtSecret = "session-write-rate-limit-tests-signing-key-0123456789";
        public const string JwtIssuer = "GoHardAPI";
        public const string JwtAudience = "GoHardApp";

        private readonly string _databaseName = "swrl-" + Guid.NewGuid().ToString("N");
        private readonly CreateCallCounter _counter = new();

        /// <summary>Extra configuration (e.g. tiny limiter limits). Set before the first client.</summary>
        public Dictionary<string, string?> ConfigOverrides { get; } = new();

        /// <summary>Total <see cref="SessionCreateService.CreateAsync"/> calls this host has seen.</summary>
        public int CreateCalls => _counter.Value;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // UseSetting (not ConfigureAppConfiguration) so values are present in
            // builder.Configuration while Program.cs's top-level statements run
            // (the JWT secret is read there, before builder.Build()).
            builder.UseSetting("JwtSettings:Secret", JwtSecret);
            builder.UseSetting("JwtSettings:Issuer", JwtIssuer);
            builder.UseSetting("JwtSettings:Audience", JwtAudience);
            builder.UseSetting("JwtSettings:ExpirationHours", "24");
            foreach (var kv in ConfigOverrides)
            {
                builder.UseSetting(kv.Key, kv.Value);
            }

            builder.ConfigureServices(services =>
            {
                foreach (var d in services.Where(s =>
                             s.ServiceType == typeof(DbContextOptions<TrainingContext>) ||
                             s.ServiceType == typeof(DbContextOptions) ||
                             s.ServiceType == typeof(TrainingContext)).ToList())
                {
                    services.Remove(d);
                }
                RegisterDatabase(services);

                foreach (var d in services
                             .Where(s => s.ImplementationType == typeof(DraftSessionCleanupService))
                             .ToList())
                {
                    services.Remove(d);
                }

                services.AddSingleton(_counter);
                services.RemoveAll<SessionCreateService>();
                services.AddScoped<SessionCreateService>(sp => new SpySessionCreateService(
                    sp.GetRequiredService<TrainingContext>(),
                    sp.GetService<ILogger<SessionCreateService>>() ?? NullLogger<SessionCreateService>.Instance,
                    sp.GetRequiredService<CreateCallCounter>()));
            });
        }

        /// <summary>Database registration. Default: isolated in-memory. PostgreSQL subclass overrides.</summary>
        protected virtual void RegisterDatabase(IServiceCollection services) =>
            services.AddDbContext<TrainingContext>(o => o.UseInMemoryDatabase(_databaseName));

        /// <summary>Seeds <paramref name="userId"/> and returns a client bearing that user's JWT.</summary>
        public HttpClient CreateClientForUser(int userId)
        {
            SeedUser(userId);
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", MintToken(userId));
            return client;
        }

        public HttpClient CreateAnonymousClient() => CreateClient();

        public HttpClient CreateClientWithInvalidToken()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");
            return client;
        }

        public void SeedUser(int userId)
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            if (!ctx.Users.Any(u => u.Id == userId))
            {
                ctx.Users.Add(new User
                {
                    Id = userId,
                    Name = $"user{userId}",
                    Email = $"user{userId}@example.com",
                    PasswordHash = "x",
                });
                ctx.SaveChanges();
            }
        }

        public long SessionCount(int userId)
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            return ctx.Sessions.Count(s => s.UserId == userId);
        }

        public long OperationCount(int userId)
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            return ctx.SessionCreateOperations.Count(o => o.UserId == userId);
        }

        public static string MintToken(int userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
            var token = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, $"user{userId}"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                },
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    /// <summary>Process-wide (per host) count of Session-CREATE service invocations.</summary>
    public sealed class CreateCallCounter
    {
        private int _n;
        public int Value => Volatile.Read(ref _n);
        public void Bump() => Interlocked.Increment(ref _n);
    }

    /// <summary>
    /// Counts every <see cref="SessionCreateService.CreateAsync"/> call and forwards to the
    /// real implementation. Proves a rate-limited request is rejected before the controller.
    /// </summary>
    public sealed class SpySessionCreateService : SessionCreateService
    {
        private readonly CreateCallCounter _counter;

        public SpySessionCreateService(
            TrainingContext context, ILogger<SessionCreateService> logger, CreateCallCounter counter)
            : base(context, logger)
        {
            _counter = counter;
        }

        public override Task<SessionCreateOutcome> CreateAsync(
            int userId, SessionCreateRequestDto request, CancellationToken cancellationToken)
        {
            _counter.Bump();
            return base.CreateAsync(userId, request, cancellationToken);
        }
    }
}
