using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramStatusAndSourceConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceConversationId",
                table: "Programs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Programs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_SourceConversationId",
                table: "Programs",
                column: "SourceConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Programs_ChatConversations_SourceConversationId",
                table: "Programs",
                column: "SourceConversationId",
                principalTable: "ChatConversations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Programs_ChatConversations_SourceConversationId",
                table: "Programs");

            migrationBuilder.DropIndex(
                name: "IX_Programs_SourceConversationId",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "SourceConversationId",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Programs");
        }
    }
}
