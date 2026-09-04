BEGIN TRANSACTION;
GO


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

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260903195625_AddSessionCreateOperationAndClientOperationId', N'8.0.10');
GO

COMMIT;
GO
