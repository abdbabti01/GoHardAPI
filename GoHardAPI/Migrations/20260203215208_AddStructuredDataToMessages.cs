using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredDataToMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL for cross-database compatibility
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE ""ChatMessages"" ADD ""ContentType"" VARCHAR(50) NOT NULL DEFAULT 'text';
                    ALTER TABLE ""ChatMessages"" ADD ""StructuredData"" TEXT NULL;
                ");
            }
            else
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE [ChatMessages] ADD [ContentType] NVARCHAR(50) NOT NULL DEFAULT 'text';
                    ALTER TABLE [ChatMessages] ADD [StructuredData] NVARCHAR(MAX) NULL;
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE ""ChatMessages"" DROP COLUMN ""StructuredData"";
                    ALTER TABLE ""ChatMessages"" DROP COLUMN ""ContentType"";
                ");
            }
            else
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE [ChatMessages] DROP COLUMN [StructuredData];
                    ALTER TABLE [ChatMessages] DROP COLUMN [ContentType];
                ");
            }
        }
    }
}
