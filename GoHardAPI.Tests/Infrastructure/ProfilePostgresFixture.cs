using System;
using System.Threading.Tasks;
using GoHardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// One real PostgreSQL 16 container whose schema is built straight from the
    /// EF model via <c>EnsureCreated</c> — enough to exercise the real
    /// <c>IX_Users_Username</c> unique index under a concurrent username claim,
    /// and genuine concurrent Body-Metrics writes for one user against real
    /// row-level locking / transactions. No migration history is involved (this
    /// change ships no migration); deliberately separate from the
    /// migration-targeted <see cref="PostgresFixture"/> /
    /// <see cref="ProgramWorkoutOccurrenceKeyPostgresFixture"/>.
    ///
    /// Docker unreachable: constructs with <see cref="Available"/> = false locally;
    /// rethrows in CI (<c>REQUIRE_POSTGRES_TESTS=true</c>).
    /// </summary>
    public sealed class ProfilePostgresFixture : IAsyncLifetime
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

            await using var ctx = NewContext();
            await ctx.Database.EnsureCreatedAsync();

            Available = true;
        }

        public async Task DisposeAsync()
        {
            try { await _container.DisposeAsync(); }
            catch { /* may never have started */ }
        }
    }

    [CollectionDefinition(Name)]
    public sealed class ProfilePostgresCollection : ICollectionFixture<ProfilePostgresFixture>
    {
        public const string Name = "profile-postgres";
    }
}
