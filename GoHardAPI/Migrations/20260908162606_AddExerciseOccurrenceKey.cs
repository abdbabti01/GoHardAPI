using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Adds the nullable <c>Exercises.OccurrenceKey</c> column that persists a materialized
    /// Exercise's program-workout occurrence identity (see
    /// <see cref="GoHardAPI.Services.ProgramWorkoutExerciseOccurrences"/> and
    /// <see cref="GoHardAPI.Services.ProgramWorkoutSessionMaterializer"/>).
    ///
    /// <para>Single additive nullable column, no new table/index. The DDL is provider-aware
    /// (see <see cref="ExerciseOccurrenceKeySql"/>) and guarded for idempotent/repeated
    /// execution on SQL Server and PostgreSQL, matching the convention established by
    /// <see cref="AddSessionCreateOperationAndClientOperationId"/> / <see cref="SessionCreateOperationSql"/>.
    /// No data migration — legacy rows get a NULL <c>OccurrenceKey</c> and are never backfilled
    /// by this migration (identity for already-materialized Exercises is never invented
    /// retroactively; see the PR notes for the runtime self-heal that backfills
    /// <c>ProgramWorkout.ExercisesJson</c> instead).</para>
    /// </summary>
    public partial class AddExerciseOccurrenceKey : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => ExerciseOccurrenceKeySql.SqlServerUp,
                Npgsql => ExerciseOccurrenceKeySql.NpgsqlUp,
                _ => ExerciseOccurrenceKeySql.GenericUp
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = migrationBuilder.ActiveProvider switch
            {
                SqlServer => ExerciseOccurrenceKeySql.SqlServerDown,
                Npgsql => ExerciseOccurrenceKeySql.NpgsqlDown,
                _ => ExerciseOccurrenceKeySql.GenericDown
            };

            // Generic/SQLite Down is a deliberate no-op (see ExerciseOccurrenceKeySql) — skip
            // emitting an empty Sql() call.
            if (!string.IsNullOrEmpty(sql))
            {
                migrationBuilder.Sql(sql);
            }
        }
    }
}
