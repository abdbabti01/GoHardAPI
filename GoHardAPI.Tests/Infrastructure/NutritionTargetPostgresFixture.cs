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
    /// One real PostgreSQL 16 container for the nutrition-target concurrency suite
    /// (<see cref="Controllers.NutritionTargetConcurrencyPostgresTests"/>). Schema built
    /// with <c>Database.EnsureCreated()</c> from the live EF model, so the
    /// <c>NutritionGoals</c> table exists with its real partial unique index
    /// (<c>IX_NutritionGoals_UserId_Active</c>) - the actual DB-level backstop this suite
    /// exercises.
    ///
    /// If Docker is unreachable the fixture constructs but <see cref="Available"/> is
    /// <c>false</c>; <see cref="DockerRequiredFactAttribute"/> skips the tests locally and
    /// <see cref="PostgresRequirement"/> makes them a hard failure in CI.
    /// </summary>
    public sealed class NutritionTargetPostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        public bool Available { get; private set; }
        public string ConnectionString { get; private set; } = string.Empty;

        public TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseNpgsql(ConnectionString).Options);

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

                // The partial unique index this suite exercises is added by
                // AddNutritionGoalEffectiveDate as raw, provider-branched SQL (not a Fluent
                // API HasIndex on NutritionGoal), because EF's scaffolder can't express a
                // Postgres partial index directly - see that migration's own doc comment.
                // EnsureCreated builds schema purely from the EF model, so it never applies
                // migration-only raw SQL; running the full migration chain from empty is a
                // separate known issue for an unrelated earlier migration (see
                // GoalArchiveMealPlanMigrationPostgresTests), so - exactly like that
                // suite's own isolation technique - this fixture applies just this one
                // index directly rather than the whole chain. Migration application itself
                // (including the backfill) is separately proven end-to-end by
                // NutritionGoalEffectiveDateMigrationPostgresTests.
                await ctx.Database.ExecuteSqlRawAsync(
                    "CREATE UNIQUE INDEX \"IX_NutritionGoals_UserId_Active\" ON \"NutritionGoals\" (\"UserId\") WHERE \"IsActive\" = TRUE;");
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
    public sealed class NutritionTargetPostgresCollection
        : ICollectionFixture<NutritionTargetPostgresFixture>
    {
        public const string Name = "nutrition-target-postgres";
    }
}
