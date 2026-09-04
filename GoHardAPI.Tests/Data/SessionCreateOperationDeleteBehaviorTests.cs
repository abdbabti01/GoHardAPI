using GoHardAPI.Data;
using GoHardAPI.Migrations;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace GoHardAPI.Tests.Data
{
    /// <summary>
    /// The <c>SessionCreateOperations -> Users</c> foreign key must have the SAME delete
    /// behavior in the EF model and in the deployed (provider-branched) DDL:
    ///   PostgreSQL / SQLite : <c>CASCADE</c>;
    ///   SQL Server          : <c>NO ACTION</c> (SQL Server rejects multiple cascade paths
    ///                         into one table - Users already cascades to Sessions).
    /// Model and deployed schema must never be knowingly contradictory.
    /// A mutation reverting <c>TrainingContext</c> to an unconditional <c>Cascade</c> breaks
    /// <see cref="Model_SqlServer_UsesNoActionForUserFk"/>.
    /// </summary>
    public class SessionCreateOperationDeleteBehaviorTests
    {
        private static IForeignKey UserForeignKey(DbContextOptions<TrainingContext> options)
        {
            using var ctx = new TrainingContext(options);
            var entity = ctx.Model.FindEntityType(typeof(SessionCreateOperation))!;
            return entity.GetForeignKeys()
                .Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        }

        private static DbContextOptions<TrainingContext> Npgsql() =>
            new DbContextOptionsBuilder<TrainingContext>()
                .UseNpgsql("Host=localhost;Database=x;Username=x;Password=x").Options;

        private static DbContextOptions<TrainingContext> SqlServer() =>
            new DbContextOptionsBuilder<TrainingContext>()
                .UseSqlServer("Server=localhost;Database=x;Trusted_Connection=True;").Options;

        private static DbContextOptions<TrainingContext> Sqlite() =>
            new DbContextOptionsBuilder<TrainingContext>()
                .UseSqlite("DataSource=:memory:").Options;

        [Fact]
        public void Model_Npgsql_CascadesUserFk()
        {
            Assert.Equal(DeleteBehavior.Cascade, UserForeignKey(Npgsql()).DeleteBehavior);
        }

        [Fact]
        public void Model_Sqlite_CascadesUserFk()
        {
            Assert.Equal(DeleteBehavior.Cascade, UserForeignKey(Sqlite()).DeleteBehavior);
        }

        [Fact]
        public void Model_SqlServer_UsesNoActionForUserFk()
        {
            Assert.Equal(DeleteBehavior.NoAction, UserForeignKey(SqlServer()).DeleteBehavior);
        }

        [Fact]
        public void Model_SessionFk_IsAlwaysSetNull()
        {
            foreach (var options in new[] { Npgsql(), SqlServer(), Sqlite() })
            {
                using var ctx = new TrainingContext(options);
                var entity = ctx.Model.FindEntityType(typeof(SessionCreateOperation))!;
                var sessionFk = entity.GetForeignKeys()
                    .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Session));
                Assert.Equal(DeleteBehavior.SetNull, sessionFk.DeleteBehavior);
            }
        }

        [Fact]
        public void Ddl_UserFk_MatchesTheModelPerProvider()
        {
            Assert.Contains(
                "FK_SessionCreateOperations_Users_UserId\" FOREIGN KEY (\"UserId\") REFERENCES \"Users\" (\"Id\") ON DELETE CASCADE",
                SessionCreateOperationSql.NpgsqlUp);
            Assert.Contains(
                "FK_SessionCreateOperations_Users_UserId\" FOREIGN KEY (\"UserId\") REFERENCES \"Users\" (\"Id\") ON DELETE CASCADE",
                SessionCreateOperationSql.GenericUp);
            Assert.Contains(
                "FK_SessionCreateOperations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION",
                SessionCreateOperationSql.SqlServerUp);
        }

        [Fact]
        public void Ddl_SessionFk_IsSetNullOnEveryProvider()
        {
            Assert.Contains("REFERENCES \"Sessions\" (\"Id\") ON DELETE SET NULL", SessionCreateOperationSql.NpgsqlUp);
            Assert.Contains("REFERENCES \"Sessions\" (\"Id\") ON DELETE SET NULL", SessionCreateOperationSql.GenericUp);
            Assert.Contains("REFERENCES [Sessions] ([Id]) ON DELETE SET NULL", SessionCreateOperationSql.SqlServerUp);
        }
    }
}
