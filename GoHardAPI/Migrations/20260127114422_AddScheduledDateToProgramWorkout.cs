using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledDateToProgramWorkout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to handle both SQL Server and PostgreSQL
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    "ALTER TABLE \"ProgramWorkouts\" ADD COLUMN \"ScheduledDate\" timestamp without time zone NULL;");
            }
            else
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "ScheduledDate",
                    table: "ProgramWorkouts",
                    type: "datetime2",
                    nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "ProgramWorkouts");
        }
    }
}
