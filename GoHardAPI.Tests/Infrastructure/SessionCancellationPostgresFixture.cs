using System;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// One real PostgreSQL 16 container for the delete-during-session-create convergence
    /// suite (<see cref="Controllers.SessionCreateCancellationPostgresTests"/>).
    ///
    /// <para>The schema is built with <c>Database.EnsureCreated()</c> from the live EF model,
    /// so every table the cancel path can touch — <c>Sessions</c>,
    /// <c>SessionCreateOperations</c>, <c>Exercises</c>, <c>ExerciseSets</c> — exists with
    /// the exact column mapping and the exact provider-aware delete behavior
    /// (<c>Exercises</c> → <c>ExerciseSets</c> <c>ON DELETE CASCADE</c>;
    /// <c>SessionCreateOperations.SessionId</c> <c>ON DELETE SET NULL</c>). That is the same
    /// mechanism the production database was originally created with, so it is a faithful
    /// stand-in for exercising the real cascade and the real
    /// <c>pg_advisory_xact_lock</c> serialization.</para>
    ///
    /// <para>The module initializer (<see cref="TestModuleInit"/>) sets
    /// <c>Npgsql.EnableLegacyTimestampBehavior</c>, so timestamps map to
    /// <c>timestamp without time zone</c> exactly as in production.</para>
    ///
    /// If Docker is unreachable the fixture constructs but <see cref="Available"/> is
    /// <c>false</c>; <see cref="DockerRequiredFactAttribute"/> skips the tests locally and
    /// <see cref="PostgresRequirement"/> makes them a hard failure in CI.
    /// </summary>
    public sealed class SessionCancellationPostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        public bool Available { get; private set; }
        public string ConnectionString { get; private set; } = string.Empty;

        public TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(ConnectionString).Options);

        public NpgsqlConnection NewRawConnection() => new(ConnectionString);

        public async Task InitializeAsync()
        {
            try
            {
                await _container.StartAsync();
            }
            catch (Exception ex)
            {
                PostgresRequirement.ThrowIfRequired(ex);
                Available = false;
                return;
            }

            ConnectionString = _container.GetConnectionString();

            await using (var ctx = NewContext())
            {
                await ctx.Database.EnsureCreatedAsync();
            }

            Available = true;
        }

        public async Task DisposeAsync()
        {
            try { await _container.DisposeAsync(); }
            catch { /* container may never have started */ }
        }

        private static int _seq;

        /// <summary>Inserts a fresh user and returns its database-assigned id.</summary>
        public async Task<int> SeedUserAsync()
        {
            var n = Interlocked.Increment(ref _seq);
            await using var ctx = NewContext();
            var user = new GoHardAPI.Models.User
            {
                Name = "user",
                Username = $"u{n}_{Guid.NewGuid().ToString("N")[..8]}",
                Email = $"u{n}_{Guid.NewGuid().ToString("N")[..8]}@example.com",
                PasswordHash = "x",
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user.Id;
        }
    }

    [CollectionDefinition(Name)]
    public sealed class SessionCancellationPostgresCollection
        : ICollectionFixture<SessionCancellationPostgresFixture>
    {
        public const string Name = "session-cancellation-postgres";
    }
}
