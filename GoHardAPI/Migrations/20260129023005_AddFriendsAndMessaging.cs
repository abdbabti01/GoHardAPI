using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendsAndMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add Username column as nullable first
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                maxLength: 30,
                nullable: true);

            // Step 2: Generate unique usernames for existing users (user_<id>)
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""Username"" = 'user_' || CAST(""Id"" AS VARCHAR)
                WHERE ""Username"" IS NULL OR ""Username"" = ''
            ");

            // Step 3: Make column required
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DirectMessages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderId = table.Column<int>(nullable: false),
                    ReceiverId = table.Column<int>(nullable: false),
                    Content = table.Column<string>(maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(nullable: false),
                    ReadAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectMessages_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DirectMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DMConversations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User1Id = table.Column<int>(nullable: false),
                    User2Id = table.Column<int>(nullable: false),
                    LastMessageAt = table.Column<DateTime>(nullable: false),
                    UnreadCountUser1 = table.Column<int>(nullable: false),
                    UnreadCountUser2 = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DMConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DMConversations_Users_User1Id",
                        column: x => x.User1Id,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DMConversations_Users_User2Id",
                        column: x => x.User2Id,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequesterId = table.Column<int>(nullable: false),
                    AddresseeId = table.Column<int>(nullable: false),
                    Status = table.Column<string>(maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(nullable: false),
                    RespondedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Friendships_Users_AddresseeId",
                        column: x => x.AddresseeId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Friendships_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_ReceiverId",
                table: "DirectMessages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectMessages_SenderId_ReceiverId_SentAt",
                table: "DirectMessages",
                columns: new[] { "SenderId", "ReceiverId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DMConversations_User1Id_User2Id",
                table: "DMConversations",
                columns: new[] { "User1Id", "User2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DMConversations_User2Id",
                table: "DMConversations",
                column: "User2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_AddresseeId_Status",
                table: "Friendships",
                columns: new[] { "AddresseeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_RequesterId_AddresseeId",
                table: "Friendships",
                columns: new[] { "RequesterId", "AddresseeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectMessages");

            migrationBuilder.DropTable(
                name: "DMConversations");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");
        }
    }
}
