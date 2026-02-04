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
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ChatMessages",
                maxLength: 50,
                nullable: false,
                defaultValue: "text");

            migrationBuilder.AddColumn<string>(
                name: "StructuredData",
                table: "ChatMessages",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "StructuredData",
                table: "ChatMessages");
        }
    }
}
