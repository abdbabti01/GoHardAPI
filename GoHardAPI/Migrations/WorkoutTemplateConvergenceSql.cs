namespace GoHardAPI.Migrations
{
    /// <summary>
    /// Provider-specific SQL for <see cref="AddWorkoutTemplateVisibilityAndConvergeSchema"/>,
    /// factored out so the schema-convergence statements can be reviewed in one place and
    /// exercised directly by the migration test-suite.
    ///
    /// The <b>SqlServer</b> and <b>Npgsql</b> branches (the only ones that run in production) are
    /// fully idempotent: tables, indexes and the <c>IsPublic</c> column are each created only when
    /// absent, so the migration is safe whether the tables already exist (provisioned by
    /// <c>Scripts/Phase3_DatabaseSetup.sql</c>), partially exist, or do not exist at all (a fresh
    /// database, or PostgreSQL where no script ever ran). The <b>Generic</b> branch (SQLite, used
    /// only by the migration test-suite) is idempotent for the table/index <c>CREATE</c>s but
    /// uses a bare <c>ALTER TABLE ... ADD COLUMN "IsPublic"</c> because SQLite has no
    /// <c>ADD COLUMN IF NOT EXISTS</c>; that is fine because EF never re-runs an applied
    /// migration and the table it adds the column to is only ever created (without the column)
    /// immediately above in the same branch, or by a test simulating the pre-upgrade schema.
    /// </summary>
    internal static class WorkoutTemplateConvergenceSql
    {
        // ------------------------------------------------------------------ SQL Server (local dev)

        internal const string SqlServerUp = @"
IF OBJECT_ID(N'[WorkoutTemplates]', N'U') IS NULL
BEGIN
    CREATE TABLE [WorkoutTemplates] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ExercisesJson] nvarchar(max) NOT NULL,
        [RecurrencePattern] nvarchar(20) NOT NULL,
        [DaysOfWeek] nvarchar(20) NULL,
        [IntervalDays] int NULL,
        [EstimatedDuration] int NULL,
        [Category] nvarchar(50) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [UsageCount] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [LastUsedAt] datetime2 NULL,
        [CreatedByUserId] int NULL,
        [IsCustom] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Rating] float NULL,
        [RatingCount] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_WorkoutTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkoutTemplates_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF OBJECT_ID(N'[WorkoutTemplateRatings]', N'U') IS NULL
BEGIN
    CREATE TABLE [WorkoutTemplateRatings] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [WorkoutTemplateId] int NOT NULL,
        [UserId] int NOT NULL,
        [Rating] float NOT NULL,
        [RatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_WorkoutTemplateRatings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkoutTemplateRatings_WorkoutTemplates_WorkoutTemplateId] FOREIGN KEY ([WorkoutTemplateId]) REFERENCES [WorkoutTemplates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkoutTemplateRatings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_WorkoutTemplates_CreatedByUserId' AND object_id = OBJECT_ID(N'[WorkoutTemplates]'))
    CREATE INDEX [IX_WorkoutTemplates_CreatedByUserId] ON [WorkoutTemplates] ([CreatedByUserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_WorkoutTemplates_IsCustom_IsActive_Category' AND object_id = OBJECT_ID(N'[WorkoutTemplates]'))
    CREATE INDEX [IX_WorkoutTemplates_IsCustom_IsActive_Category] ON [WorkoutTemplates] ([IsCustom], [IsActive], [Category]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_WorkoutTemplateRatings_UserId' AND object_id = OBJECT_ID(N'[WorkoutTemplateRatings]'))
    CREATE INDEX [IX_WorkoutTemplateRatings_UserId] ON [WorkoutTemplateRatings] ([UserId]);

-- Only create the EF-conventionally-named unique index when no equivalent one already exists
-- (Scripts/Phase3_DatabaseSetup.sql created it as IX_WorkoutTemplateRatings_TemplateId_UserId).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] IN (N'IX_WorkoutTemplateRatings_WorkoutTemplateId_UserId', N'IX_WorkoutTemplateRatings_TemplateId_UserId') AND object_id = OBJECT_ID(N'[WorkoutTemplateRatings]'))
    CREATE UNIQUE INDEX [IX_WorkoutTemplateRatings_WorkoutTemplateId_UserId] ON [WorkoutTemplateRatings] ([WorkoutTemplateId], [UserId]);

IF COL_LENGTH(N'[WorkoutTemplates]', N'IsPublic') IS NULL
    ALTER TABLE [WorkoutTemplates] ADD [IsPublic] bit NOT NULL DEFAULT CAST(0 AS bit);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_WorkoutTemplates_IsPublic_Category' AND object_id = OBJECT_ID(N'[WorkoutTemplates]'))
    CREATE INDEX [IX_WorkoutTemplates_IsPublic_Category] ON [WorkoutTemplates] ([IsPublic], [Category]);
";

        internal const string SqlServerDown = @"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_WorkoutTemplates_IsPublic_Category' AND object_id = OBJECT_ID(N'[WorkoutTemplates]'))
    DROP INDEX [IX_WorkoutTemplates_IsPublic_Category] ON [WorkoutTemplates];

DECLARE @defaultConstraint nvarchar(200);
SELECT @defaultConstraint = dc.[name]
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE c.object_id = OBJECT_ID(N'[WorkoutTemplates]') AND c.[name] = N'IsPublic';
IF @defaultConstraint IS NOT NULL
    EXEC('ALTER TABLE [WorkoutTemplates] DROP CONSTRAINT [' + @defaultConstraint + ']');

IF COL_LENGTH(N'[WorkoutTemplates]', N'IsPublic') IS NOT NULL
    ALTER TABLE [WorkoutTemplates] DROP COLUMN [IsPublic];
";

        // ---------------------------------------------------------------- PostgreSQL (production)

        internal const string NpgsqlUp = @"
CREATE TABLE IF NOT EXISTS ""WorkoutTemplates"" (
    ""Id"" serial NOT NULL,
    ""Name"" character varying(100) NOT NULL,
    ""Description"" character varying(500) NULL,
    ""ExercisesJson"" text NOT NULL,
    ""RecurrencePattern"" character varying(20) NOT NULL,
    ""DaysOfWeek"" character varying(20) NULL,
    ""IntervalDays"" integer NULL,
    ""EstimatedDuration"" integer NULL,
    ""Category"" character varying(50) NULL,
    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
    ""UsageCount"" integer NOT NULL DEFAULT 0,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
    ""LastUsedAt"" timestamp with time zone NULL,
    ""CreatedByUserId"" integer NULL,
    ""IsCustom"" boolean NOT NULL DEFAULT FALSE,
    ""Rating"" double precision NULL,
    ""RatingCount"" integer NOT NULL DEFAULT 0,
    CONSTRAINT ""PK_WorkoutTemplates"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_WorkoutTemplates_Users_CreatedByUserId"" FOREIGN KEY (""CreatedByUserId"") REFERENCES ""Users"" (""Id"") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS ""WorkoutTemplateRatings"" (
    ""Id"" serial NOT NULL,
    ""WorkoutTemplateId"" integer NOT NULL,
    ""UserId"" integer NOT NULL,
    ""Rating"" double precision NOT NULL,
    ""RatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT ""PK_WorkoutTemplateRatings"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_WorkoutTemplateRatings_WorkoutTemplates_WorkoutTemplateId"" FOREIGN KEY (""WorkoutTemplateId"") REFERENCES ""WorkoutTemplates"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_WorkoutTemplateRatings_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE NO ACTION
);

CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplates_CreatedByUserId"" ON ""WorkoutTemplates"" (""CreatedByUserId"");
CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplates_IsCustom_IsActive_Category"" ON ""WorkoutTemplates"" (""IsCustom"", ""IsActive"", ""Category"");
CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplateRatings_UserId"" ON ""WorkoutTemplateRatings"" (""UserId"");

-- Unlike the SQL Server branch (which must also skip the Phase3 script's
-- IX_WorkoutTemplateRatings_TemplateId_UserId), PostgreSQL has no hand-provisioned copy of
-- these tables, so there is no alternative index name to guard against: IF NOT EXISTS on the
-- EF name is sufficient. Worst case if that ever changes is a redundant second unique index
-- over the same columns, which still enforces the constraint correctly.
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WorkoutTemplateRatings_WorkoutTemplateId_UserId"" ON ""WorkoutTemplateRatings"" (""WorkoutTemplateId"", ""UserId"");

ALTER TABLE ""WorkoutTemplates"" ADD COLUMN IF NOT EXISTS ""IsPublic"" boolean NOT NULL DEFAULT FALSE;

CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplates_IsPublic_Category"" ON ""WorkoutTemplates"" (""IsPublic"", ""Category"");
";

        internal const string NpgsqlDown = @"
DROP INDEX IF EXISTS ""IX_WorkoutTemplates_IsPublic_Category"";
ALTER TABLE ""WorkoutTemplates"" DROP COLUMN IF EXISTS ""IsPublic"";
";

        // ------------------------------------------ Generic (SQLite test-suite / other providers)

        internal const string GenericUp = @"
CREATE TABLE IF NOT EXISTS ""WorkoutTemplates"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WorkoutTemplates"" PRIMARY KEY AUTOINCREMENT,
    ""Name"" TEXT NOT NULL,
    ""Description"" TEXT NULL,
    ""ExercisesJson"" TEXT NOT NULL,
    ""RecurrencePattern"" TEXT NOT NULL,
    ""DaysOfWeek"" TEXT NULL,
    ""IntervalDays"" INTEGER NULL,
    ""EstimatedDuration"" INTEGER NULL,
    ""Category"" TEXT NULL,
    ""IsActive"" INTEGER NOT NULL DEFAULT 1,
    ""UsageCount"" INTEGER NOT NULL DEFAULT 0,
    ""CreatedAt"" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
    ""LastUsedAt"" TEXT NULL,
    ""CreatedByUserId"" INTEGER NULL,
    ""IsCustom"" INTEGER NOT NULL DEFAULT 0,
    ""Rating"" REAL NULL,
    ""RatingCount"" INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT ""FK_WorkoutTemplates_Users_CreatedByUserId"" FOREIGN KEY (""CreatedByUserId"") REFERENCES ""Users"" (""Id"") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS ""WorkoutTemplateRatings"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WorkoutTemplateRatings"" PRIMARY KEY AUTOINCREMENT,
    ""WorkoutTemplateId"" INTEGER NOT NULL,
    ""UserId"" INTEGER NOT NULL,
    ""Rating"" REAL NOT NULL,
    ""RatedAt"" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00',
    CONSTRAINT ""FK_WorkoutTemplateRatings_WorkoutTemplates_WorkoutTemplateId"" FOREIGN KEY (""WorkoutTemplateId"") REFERENCES ""WorkoutTemplates"" (""Id"") ON DELETE CASCADE,
    CONSTRAINT ""FK_WorkoutTemplateRatings_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE NO ACTION
);

CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplates_CreatedByUserId"" ON ""WorkoutTemplates"" (""CreatedByUserId"");
CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplates_IsCustom_IsActive_Category"" ON ""WorkoutTemplates"" (""IsCustom"", ""IsActive"", ""Category"");
CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplateRatings_UserId"" ON ""WorkoutTemplateRatings"" (""UserId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WorkoutTemplateRatings_WorkoutTemplateId_UserId"" ON ""WorkoutTemplateRatings"" (""WorkoutTemplateId"", ""UserId"");

ALTER TABLE ""WorkoutTemplates"" ADD COLUMN ""IsPublic"" INTEGER NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS ""IX_WorkoutTemplates_IsPublic_Category"" ON ""WorkoutTemplates"" (""IsPublic"", ""Category"");
";

        internal const string GenericDown = @"
DROP INDEX IF EXISTS ""IX_WorkoutTemplates_IsPublic_Category"";
ALTER TABLE ""WorkoutTemplates"" DROP COLUMN ""IsPublic"";
";
    }
}
