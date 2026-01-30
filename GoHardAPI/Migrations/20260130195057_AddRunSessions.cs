using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRunSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL for PostgreSQL compatibility
            migrationBuilder.Sql(@"
                CREATE TABLE ""RunSessions"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" INTEGER NOT NULL,
                    ""Name"" VARCHAR(100) NULL,
                    ""Date"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""Distance"" DOUBLE PRECISION NULL,
                    ""Duration"" INTEGER NULL,
                    ""AveragePace"" DOUBLE PRECISION NULL,
                    ""Calories"" INTEGER NULL,
                    ""Status"" VARCHAR(20) NOT NULL,
                    ""StartedAt"" TIMESTAMP WITH TIME ZONE NULL,
                    ""CompletedAt"" TIMESTAMP WITH TIME ZONE NULL,
                    ""PausedAt"" TIMESTAMP WITH TIME ZONE NULL,
                    ""RouteJson"" TEXT NULL,
                    CONSTRAINT ""FK_RunSessions_Users_UserId"" FOREIGN KEY (""UserId"")
                        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX ""IX_RunSessions_UserId_Date"" ON ""RunSessions"" (""UserId"", ""Date"");
                CREATE INDEX ""IX_RunSessions_UserId_Status"" ON ""RunSessions"" (""UserId"", ""Status"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""RunSessions"";");
        }
    }
}
