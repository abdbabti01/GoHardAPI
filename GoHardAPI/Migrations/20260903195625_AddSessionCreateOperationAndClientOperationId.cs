using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Server-side foundation for keyed Session CREATE.
    ///
    /// Adds:
    /// <list type="bullet">
    ///   <item>a nullable <c>Sessions.ClientOperationId</c> (PostgreSQL <c>uuid</c>,
    ///     SQL Server <c>uniqueidentifier</c>, SQLite GUID-as-text) — legacy NULL rows are
    ///     left untouched and need no backfill;</item>
    ///   <item>an owner-scoped <b>partial</b> unique index
    ///     <c>IX_Sessions_UserId_ClientOperationId</c> on <c>(UserId, ClientOperationId)</c>
    ///     <c>WHERE ClientOperationId IS NOT NULL</c>, so many NULL rows coexist;</item>
    ///   <item>the durable <c>SessionCreateOperations</c> table — one row per
    ///     <c>(UserId, ClientOperationId)</c>, with <c>SessionId</c> nullable and
    ///     <c>ON DELETE SET NULL</c> so a vanished Session is reported as 410 Gone rather
    ///     than silently recreated.</item>
    /// </list>
    ///
    /// The DDL is provider-aware (see <see cref="SessionCreateOperationSql"/>): the raw
    /// scaffold is SQL Server only, but production is PostgreSQL/Npgsql. Each branch is a
    /// single non-suppressed <c>Sql()</c> call, so EF wraps the schema change and the
    /// <c>__EFMigrationsHistory</c> row in one transaction.
    /// </summary>
    public partial class AddSessionCreateOperationAndClientOperationId : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SessionCreateOperationSql.SqlServerUp,
                Npgsql => SessionCreateOperationSql.NpgsqlUp,
                _ => SessionCreateOperationSql.GenericUp
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SessionCreateOperationSql.SqlServerDown,
                Npgsql => SessionCreateOperationSql.NpgsqlDown,
                _ => SessionCreateOperationSql.GenericDown
            });
        }
    }
}
