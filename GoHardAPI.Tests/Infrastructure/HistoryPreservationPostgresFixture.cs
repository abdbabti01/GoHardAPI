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
    /// current EF model via <c>EnsureCreated</c> (this feature ships an additive
    /// migration, but these tests exercise runtime controller behavior — Goal/Program
    /// deletion-preserves-history, archive semantics, and non-destructive AI
    /// meal-plan application — not migration application itself, so a fresh
    /// model-derived schema is sufficient and simpler; mirrors
    /// <see cref="ProfilePostgresFixture"/>'s approach).
    ///
    /// Docker unreachable: constructs with <see cref="Available"/> = false locally;
    /// rethrows in CI (<c>REQUIRE_POSTGRES_TESTS=true</c>).
    /// </summary>
    public sealed class HistoryPreservationPostgresFixture : IAsyncLifetime
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
    public sealed class HistoryPreservationPostgresCollection : ICollectionFixture<HistoryPreservationPostgresFixture>
    {
        public const string Name = "history-preservation-postgres";
    }
}
