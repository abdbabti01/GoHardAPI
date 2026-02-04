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
            // Use raw SQL for cross-database compatibility
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE ""Programs"" ADD ""SourceConversationId"" INTEGER NULL;
                    ALTER TABLE ""Programs"" ADD ""Status"" VARCHAR(20) NOT NULL DEFAULT 'active';
                    CREATE INDEX ""IX_Programs_SourceConversationId"" ON ""Programs"" (""SourceConversationId"");
                    ALTER TABLE ""Programs"" ADD CONSTRAINT ""FK_Programs_ChatConversations_SourceConversationId""
                        FOREIGN KEY (""SourceConversationId"") REFERENCES ""ChatConversations"" (""Id"");
                ");
            }
            else
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE [Programs] ADD [SourceConversationId] INT NULL;
                    ALTER TABLE [Programs] ADD [Status] NVARCHAR(20) NOT NULL DEFAULT 'active';
                    CREATE INDEX [IX_Programs_SourceConversationId] ON [Programs] ([SourceConversationId]);
                    ALTER TABLE [Programs] ADD CONSTRAINT [FK_Programs_ChatConversations_SourceConversationId]
                        FOREIGN KEY ([SourceConversationId]) REFERENCES [ChatConversations] ([Id]);
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE ""Programs"" DROP CONSTRAINT ""FK_Programs_ChatConversations_SourceConversationId"";
                    DROP INDEX ""IX_Programs_SourceConversationId"";
                    ALTER TABLE ""Programs"" DROP COLUMN ""SourceConversationId"";
                    ALTER TABLE ""Programs"" DROP COLUMN ""Status"";
                ");
            }
            else
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE [Programs] DROP CONSTRAINT [FK_Programs_ChatConversations_SourceConversationId];
                    DROP INDEX [IX_Programs_SourceConversationId] ON [Programs];
                    ALTER TABLE [Programs] DROP COLUMN [SourceConversationId];
                    ALTER TABLE [Programs] DROP COLUMN [Status];
                ");
            }
        }
    }
}
