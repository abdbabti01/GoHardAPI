using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoHardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalArchiveAndMealPlanSourceIdentity : Migration
    {
        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        // Raw, provider-branched SQL rather than the builder's `AddColumn<T>(type: ...)`
        // overload: an explicit `type:` string is used VERBATIM by whichever provider
        // actually executes the migration (it is not translated per-provider), so a
        // literal SQL Server type like "datetime2"/"bit" fails outright on PostgreSQL
        // ("bit" also means something different there - a fixed-length bit string, not a
        // boolean) - and, verified empirically, simply omitting `type:` still resolved to
        // "datetime2" against PostgreSQL in this codebase's toolchain rather than falling
        // back to Npgsql's native type mapper. Writing the DDL explicitly per provider,
        // matching this repo's established pattern for exactly this reason (see
        // `AddExerciseOccurrenceKey` / `AddSessionCreateOperationAndClientOperationId`),
        // is the only technique confirmed by a real PostgreSQL test
        // (`GoalArchiveMealPlanMigrationPostgresTests`) to apply correctly on both
        // providers, per CLAUDE.md's "must work correctly on both providers" rule.
        private const string SqlServerUp = @"
ALTER TABLE [Goals] ADD [ArchivedAt] datetime2 NULL;
ALTER TABLE [Goals] ADD [IsArchived] bit NOT NULL DEFAULT 0;
ALTER TABLE [FoodItems] ADD [SourcePlanConversationId] int NULL;
ALTER TABLE [FoodItems] ADD [SourcePlanDay] int NULL;

DROP INDEX [IX_FoodItems_MealEntryId] ON [FoodItems];

CREATE INDEX [IX_Goals_UserId_IsArchived] ON [Goals] ([UserId], [IsArchived]);
CREATE INDEX [IX_FoodItems_MealEntryId_SourcePlanConversationId_SourcePlanDay] ON [FoodItems] ([MealEntryId], [SourcePlanConversationId], [SourcePlanDay]);
CREATE INDEX [IX_FoodItems_SourcePlanConversationId] ON [FoodItems] ([SourcePlanConversationId]);

ALTER TABLE [FoodItems] ADD CONSTRAINT [FK_FoodItems_ChatConversations_SourcePlanConversationId]
    FOREIGN KEY ([SourcePlanConversationId]) REFERENCES [ChatConversations] ([Id]) ON DELETE SET NULL;
";

        private const string NpgsqlUp = @"
ALTER TABLE ""Goals"" ADD ""ArchivedAt"" timestamp with time zone NULL;
ALTER TABLE ""Goals"" ADD ""IsArchived"" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE ""FoodItems"" ADD ""SourcePlanConversationId"" integer NULL;
ALTER TABLE ""FoodItems"" ADD ""SourcePlanDay"" integer NULL;

DROP INDEX ""IX_FoodItems_MealEntryId"";

CREATE INDEX ""IX_Goals_UserId_IsArchived"" ON ""Goals"" (""UserId"", ""IsArchived"");
CREATE INDEX ""IX_FoodItems_MealEntryId_SourcePlanConversationId_SourcePlanDay"" ON ""FoodItems"" (""MealEntryId"", ""SourcePlanConversationId"", ""SourcePlanDay"");
CREATE INDEX ""IX_FoodItems_SourcePlanConversationId"" ON ""FoodItems"" (""SourcePlanConversationId"");

ALTER TABLE ""FoodItems"" ADD CONSTRAINT ""FK_FoodItems_ChatConversations_SourcePlanConversationId""
    FOREIGN KEY (""SourcePlanConversationId"") REFERENCES ""ChatConversations"" (""Id"") ON DELETE SET NULL;
";

        private const string SqlServerDown = @"
ALTER TABLE [FoodItems] DROP CONSTRAINT [FK_FoodItems_ChatConversations_SourcePlanConversationId];

DROP INDEX [IX_Goals_UserId_IsArchived] ON [Goals];
DROP INDEX [IX_FoodItems_MealEntryId_SourcePlanConversationId_SourcePlanDay] ON [FoodItems];
DROP INDEX [IX_FoodItems_SourcePlanConversationId] ON [FoodItems];

ALTER TABLE [Goals] DROP COLUMN [ArchivedAt];
ALTER TABLE [Goals] DROP COLUMN [IsArchived];
ALTER TABLE [FoodItems] DROP COLUMN [SourcePlanConversationId];
ALTER TABLE [FoodItems] DROP COLUMN [SourcePlanDay];

CREATE INDEX [IX_FoodItems_MealEntryId] ON [FoodItems] ([MealEntryId]);
";

        private const string NpgsqlDown = @"
ALTER TABLE ""FoodItems"" DROP CONSTRAINT ""FK_FoodItems_ChatConversations_SourcePlanConversationId"";

DROP INDEX ""IX_Goals_UserId_IsArchived"";
DROP INDEX ""IX_FoodItems_MealEntryId_SourcePlanConversationId_SourcePlanDay"";
DROP INDEX ""IX_FoodItems_SourcePlanConversationId"";

ALTER TABLE ""Goals"" DROP COLUMN ""ArchivedAt"";
ALTER TABLE ""Goals"" DROP COLUMN ""IsArchived"";
ALTER TABLE ""FoodItems"" DROP COLUMN ""SourcePlanConversationId"";
ALTER TABLE ""FoodItems"" DROP COLUMN ""SourcePlanDay"";

CREATE INDEX ""IX_FoodItems_MealEntryId"" ON ""FoodItems"" (""MealEntryId"");
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SqlServerUp,
                Npgsql => NpgsqlUp,
                _ => NpgsqlUp,
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(migrationBuilder.ActiveProvider switch
            {
                SqlServer => SqlServerDown,
                Npgsql => NpgsqlDown,
                _ => NpgsqlDown,
            });
        }
    }
}
