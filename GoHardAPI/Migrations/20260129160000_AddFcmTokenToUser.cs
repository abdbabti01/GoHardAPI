using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFcmTokenToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to handle both SQL Server and PostgreSQL
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    "ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"FcmToken\" VARCHAR(500) NULL;");
            }
            else
            {
                migrationBuilder.AddColumn<string>(
                    name: "FcmToken",
                    table: "Users",
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "Users");
        }
    }
}
