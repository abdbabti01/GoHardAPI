namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Provider-specific SQL for <see cref="AddSessionCreateOperationAndClientOperationId"/>,
    /// factored out so the keyed-Session-CREATE schema can be reviewed in one place and
    /// exercised directly by the migration test-suite — the same arrangement as
    /// <see cref="WorkoutTemplateConvergenceSql"/>.
    ///
    /// <para><b>Why a provider switch and not the scaffolded SQL:</b> EF scaffolds this
    /// migration against SQL Server, so the raw output carries <c>uniqueidentifier</c>,
    /// <c>datetime2</c> and a <c>[ClientOperationId] IS NOT NULL</c> filter — none of which
    /// are valid PostgreSQL, which is production. Each branch below emits the correct native
    /// types (<c>uuid</c> / <c>timestamp</c> on PostgreSQL, matching the app's
    /// <c>Npgsql.EnableLegacyTimestampBehavior</c> switch) and the correctly-quoted partial
    /// index predicate.</para>
    ///
    /// <para><b>Transaction safety:</b> each branch is emitted as a single
    /// <c>migrationBuilder.Sql(...)</c> call with no <c>suppressTransaction</c>, so EF wraps
    /// the whole migration — every DDL statement plus the <c>__EFMigrationsHistory</c> row —
    /// in one transaction. A failed deploy leaves neither a history row nor partial schema.
    /// Never add a <c>suppressTransaction: true</c> call or a PostgreSQL
    /// <c>CREATE INDEX CONCURRENTLY</c> here.</para>
    ///
    /// <para><b>Legacy rows:</b> <c>Sessions.ClientOperationId</c> is added nullable with no
    /// backfill. The owner-scoped unique index is partial (<c>WHERE ClientOperationId IS NOT
    /// NULL</c>) so any number of legacy NULL rows coexist.</para>
    ///
    /// <para><b>Delete behavior:</b> <c>SessionCreateOperations.SessionId</c> is always
    /// <c>ON DELETE SET NULL</c> — a Session cascade-delete or the draft reaper blanks the
    /// pointer but can never erase the operation record. <c>UserId</c> is
    /// <c>ON DELETE CASCADE</c> on PostgreSQL (production) and SQLite, both of which permit
    /// the resulting multiple delete paths; on SQL Server (local dev only) it is
    /// <c>ON DELETE NO ACTION</c> because SQL Server rejects multiple cascade paths into one
    /// table.</para>
    /// </summary>
    internal static class SessionCreateOperationSql
    {
        // ------------------------------------------------------------------ SQL Server (local dev)

        internal const string SqlServerUp = @"
IF COL_LENGTH(N'[Sessions]', N'ClientOperationId') IS NULL
    ALTER TABLE [Sessions] ADD [ClientOperationId] uniqueidentifier NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Sessions_UserId_ClientOperationId' AND object_id = OBJECT_ID(N'[Sessions]'))
    CREATE UNIQUE INDEX [IX_Sessions_UserId_ClientOperationId] ON [Sessions] ([UserId], [ClientOperationId]) WHERE [ClientOperationId] IS NOT NULL;

IF OBJECT_ID(N'[SessionCreateOperations]', N'U') IS NULL
BEGIN
    CREATE TABLE [SessionCreateOperations] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [ClientOperationId] uniqueidentifier NOT NULL,
        [SessionId] int NULL,
        [CompletedAt] datetime2 NULL,
        [CanceledAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SessionCreateOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SessionCreateOperations_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_SessionCreateOperations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SessionCreateOperations_SessionId' AND object_id = OBJECT_ID(N'[SessionCreateOperations]'))
    CREATE INDEX [IX_SessionCreateOperations_SessionId] ON [SessionCreateOperations] ([SessionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SessionCreateOperations_UserId_ClientOperationId' AND object_id = OBJECT_ID(N'[SessionCreateOperations]'))
    CREATE UNIQUE INDEX [IX_SessionCreateOperations_UserId_ClientOperationId] ON [SessionCreateOperations] ([UserId], [ClientOperationId]);
";

        internal const string SqlServerDown = @"
IF OBJECT_ID(N'[SessionCreateOperations]', N'U') IS NOT NULL
    DROP TABLE [SessionCreateOperations];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Sessions_UserId_ClientOperationId' AND object_id = OBJECT_ID(N'[Sessions]'))
    DROP INDEX [IX_Sessions_UserId_ClientOperationId] ON [Sessions];

IF COL_LENGTH(N'[Sessions]', N'ClientOperationId') IS NOT NULL
    ALTER TABLE [Sessions] DROP COLUMN [ClientOperationId];
";

        // ---------------------------------------------------------------- PostgreSQL (production)

        internal const string NpgsqlUp = @"
ALTER TABLE ""Sessions"" ADD COLUMN IF NOT EXISTS ""ClientOperationId"" uuid NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Sessions_UserId_ClientOperationId""
    ON ""Sessions"" (""UserId"", ""ClientOperationId"")
    WHERE ""ClientOperationId"" IS NOT NULL;

CREATE TABLE IF NOT EXISTS ""SessionCreateOperations"" (
    ""Id"" serial NOT NULL,
    ""UserId"" integer NOT NULL,
    ""ClientOperationId"" uuid NOT NULL,
    ""SessionId"" integer NULL,
    ""CompletedAt"" timestamp without time zone NULL,
    ""CanceledAt"" timestamp without time zone NULL,
    ""CreatedAt"" timestamp without time zone NOT NULL,
    CONSTRAINT ""PK_SessionCreateOperations"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_SessionCreateOperations_Sessions_SessionId"" FOREIGN KEY (""SessionId"") REFERENCES ""Sessions"" (""Id"") ON DELETE SET NULL,
    CONSTRAINT ""FK_SessionCreateOperations_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_SessionCreateOperations_SessionId"" ON ""SessionCreateOperations"" (""SessionId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SessionCreateOperations_UserId_ClientOperationId"" ON ""SessionCreateOperations"" (""UserId"", ""ClientOperationId"");
";

        internal const string NpgsqlDown = @"
DROP TABLE IF EXISTS ""SessionCreateOperations"";
DROP INDEX IF EXISTS ""IX_Sessions_UserId_ClientOperationId"";
ALTER TABLE IF EXISTS ""Sessions"" DROP COLUMN IF EXISTS ""ClientOperationId"";
";

        // ------------------------------------------ Generic (SQLite test-suite / other providers)

        internal const string GenericUp = @"
ALTER TABLE ""Sessions"" ADD COLUMN ""ClientOperationId"" TEXT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Sessions_UserId_ClientOperationId""
    ON ""Sessions"" (""UserId"", ""ClientOperationId"")
    WHERE ""ClientOperationId"" IS NOT NULL;

CREATE TABLE IF NOT EXISTS ""SessionCreateOperations"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SessionCreateOperations"" PRIMARY KEY AUTOINCREMENT,
    ""UserId"" INTEGER NOT NULL,
    ""ClientOperationId"" TEXT NOT NULL,
    ""SessionId"" INTEGER NULL,
    ""CompletedAt"" TEXT NULL,
    ""CanceledAt"" TEXT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    CONSTRAINT ""FK_SessionCreateOperations_Sessions_SessionId"" FOREIGN KEY (""SessionId"") REFERENCES ""Sessions"" (""Id"") ON DELETE SET NULL,
    CONSTRAINT ""FK_SessionCreateOperations_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_SessionCreateOperations_SessionId"" ON ""SessionCreateOperations"" (""SessionId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SessionCreateOperations_UserId_ClientOperationId"" ON ""SessionCreateOperations"" (""UserId"", ""ClientOperationId"");
";

        // Deliberately partial on the generic branch: SQLite has no
        // ""ALTER TABLE ... DROP COLUMN IF EXISTS"" and no in-SQL table guard, and a Down can
        // run in a fixture where the Up never did (e.g. an unrelated migration test that
        // sweeps the whole chain down). The left-over nullable ""ClientOperationId"" column
        // is inert once its partial unique index is gone. Forward migration is the real
        // path; see the PR's rollback notes.
        internal const string GenericDown = @"
DROP TABLE IF EXISTS ""SessionCreateOperations"";
DROP INDEX IF EXISTS ""IX_Sessions_UserId_ClientOperationId"";
";
    }
}
