using System;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GoHardAPI.Tests.Data
{
    /// <summary>
    /// <see cref="UniqueConstraintViolation.Matches"/> must recognise ONLY the
    /// index it is asked about, so a caller can safely map exactly one conflict to
    /// 409 and rethrow everything else. Uses a real SQLite unique-index violation
    /// (the provider used across this test project) for the positive case.
    /// </summary>
    public sealed class UniqueConstraintViolationTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public UniqueConstraintViolationTests()
        {
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();
            using var ctx = NewContext();
            ctx.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_conn).Options);

        private static User NewUser(int id, string username) => new()
        {
            Id = id,
            Name = $"User {id}",
            Username = username,
            Email = $"user{id}@example.com",
            PasswordHash = "hash",
            DateCreated = DateTime.UtcNow,
            UnitPreference = "Metric",
        };

        [Fact]
        public async Task matches_the_username_index_on_a_real_duplicate_and_ignores_a_different_index_name()
        {
            await using (var ctx = NewContext())
            {
                ctx.Users.Add(NewUser(1, "dup"));
                await ctx.SaveChangesAsync();
            }

            DbUpdateException captured = await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await using var ctx = NewContext();
                ctx.Users.Add(NewUser(2, "dup"));
                await ctx.SaveChangesAsync();
            });

            Assert.True(UniqueConstraintViolation.Matches(
                captured, "IX_Users_Username", "Users.Username"));

            // A caller asking about a different constraint must get false, so an
            // unrelated DbUpdateException is never misreported as "username taken".
            Assert.False(UniqueConstraintViolation.Matches(
                captured, "IX_Friendships_RequesterId_AddresseeId", "Friendships.RequesterId"));
        }

        [Fact]
        public void a_plain_exception_that_is_not_a_db_violation_never_matches()
        {
            var ex = new DbUpdateException("boom", new InvalidOperationException("not a unique violation"));
            Assert.False(UniqueConstraintViolation.Matches(ex, "IX_Users_Username", "Users.Username"));
        }
    }
}
