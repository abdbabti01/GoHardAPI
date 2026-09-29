using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Adds nullable <c>Exercises.TargetSets/TargetRepsMin/TargetRepsMax</c> (Phase 2D target
    /// snapshot). Additive, guarded, no backfill — see <see cref="ExerciseTargetsSql"/>.
    /// </summary>
    public partial class AddExerciseTargets : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => ExerciseTargetsSql.SqlServerUp,
                Npgsql => ExerciseTargetsSql.NpgsqlUp,
                _ => ExerciseTargetsSql.GenericUp
            });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = migrationBuilder.ActiveProvider switch
            {
                SqlServer => ExerciseTargetsSql.SqlServerDown,
                Npgsql => ExerciseTargetsSql.NpgsqlDown,
                _ => ExerciseTargetsSql.GenericDown
            };
            if (!string.IsNullOrEmpty(sql))
            {
                migrationBuilder.Sql(sql);
            }
        }
    }
}
