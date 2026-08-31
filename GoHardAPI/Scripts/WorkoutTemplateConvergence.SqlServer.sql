BEGIN TRANSACTION;
GO


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

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830215026_AddWorkoutTemplateVisibilityAndConvergeSchema', N'8.0.10');
GO

COMMIT;
GO
