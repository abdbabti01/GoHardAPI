-- Phase 3: Database Migration for Shared Workouts and Templates
-- This script safely adds new tables and updates existing ones

USE TrainingAppDb;
GO

-- Step 1: Create SharedWorkouts table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SharedWorkouts')
BEGIN
    CREATE TABLE SharedWorkouts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OriginalId INT NOT NULL,
        Type NVARCHAR(20) NOT NULL,
        SharedByUserId INT NOT NULL,
        WorkoutName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        ExercisesJson NVARCHAR(MAX) NOT NULL,
        Duration INT NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Difficulty NVARCHAR(20) NULL,
        LikeCount INT NOT NULL DEFAULT 0,
        SaveCount INT NOT NULL DEFAULT 0,
        CommentCount INT NOT NULL DEFAULT 0,
        SharedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_SharedWorkouts_Users FOREIGN KEY (SharedByUserId)
            REFERENCES Users(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_SharedWorkouts_Category_SharedAt ON SharedWorkouts(Category, SharedAt);

    PRINT 'Created SharedWorkouts table';
END
ELSE
BEGIN
    PRINT 'SharedWorkouts table already exists';
END
GO

-- Step 2: Create SharedWorkoutLikes table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SharedWorkoutLikes')
BEGIN
    CREATE TABLE SharedWorkoutLikes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SharedWorkoutId INT NOT NULL,
        UserId INT NOT NULL,
        LikedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_SharedWorkoutLikes_SharedWorkout FOREIGN KEY (SharedWorkoutId)
            REFERENCES SharedWorkouts(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SharedWorkoutLikes_User FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_SharedWorkoutLikes_SharedWorkoutId_UserId
        ON SharedWorkoutLikes(SharedWorkoutId, UserId);

    PRINT 'Created SharedWorkoutLikes table';
END
ELSE
BEGIN
    PRINT 'SharedWorkoutLikes table already exists';
END
GO

-- Step 3: Create SharedWorkoutSaves table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SharedWorkoutSaves')
BEGIN
    CREATE TABLE SharedWorkoutSaves (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SharedWorkoutId INT NOT NULL,
        UserId INT NOT NULL,
        SavedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_SharedWorkoutSaves_SharedWorkout FOREIGN KEY (SharedWorkoutId)
            REFERENCES SharedWorkouts(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SharedWorkoutSaves_User FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_SharedWorkoutSaves_SharedWorkoutId_UserId
        ON SharedWorkoutSaves(SharedWorkoutId, UserId);

    PRINT 'Created SharedWorkoutSaves table';
END
ELSE
BEGIN
    PRINT 'SharedWorkoutSaves table already exists';
END
GO

-- Step 4: Update WorkoutTemplates table if it already exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkoutTemplates')
BEGIN
    -- Add missing columns to existing table
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'IsCustom')
        ALTER TABLE WorkoutTemplates ADD IsCustom BIT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'Rating')
        ALTER TABLE WorkoutTemplates ADD Rating FLOAT NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'RatingCount')
        ALTER TABLE WorkoutTemplates ADD RatingCount INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'RecurrencePattern')
        ALTER TABLE WorkoutTemplates ADD RecurrencePattern NVARCHAR(20) NOT NULL DEFAULT 'daily';

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'DaysOfWeek')
        ALTER TABLE WorkoutTemplates ADD DaysOfWeek NVARCHAR(20) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'IntervalDays')
        ALTER TABLE WorkoutTemplates ADD IntervalDays INT NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'EstimatedDuration')
        ALTER TABLE WorkoutTemplates ADD EstimatedDuration INT NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'IsActive')
        ALTER TABLE WorkoutTemplates ADD IsActive BIT NOT NULL DEFAULT 1;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'UsageCount')
        ALTER TABLE WorkoutTemplates ADD UsageCount INT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'LastUsedAt')
        ALTER TABLE WorkoutTemplates ADD LastUsedAt DATETIME2 NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'CreatedByUserId')
        ALTER TABLE WorkoutTemplates ADD CreatedByUserId INT NULL;

    -- Add foreign key if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkoutTemplates_Users')
        ALTER TABLE WorkoutTemplates
            ADD CONSTRAINT FK_WorkoutTemplates_Users FOREIGN KEY (CreatedByUserId)
            REFERENCES Users(Id) ON DELETE SET NULL;

    -- Add index if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutTemplates_IsCustom_IsActive_Category')
        CREATE INDEX IX_WorkoutTemplates_IsCustom_IsActive_Category
            ON WorkoutTemplates(IsCustom, IsActive, Category);

    PRINT 'Updated WorkoutTemplates table with new columns';
END
ELSE
BEGIN
    -- Create WorkoutTemplates table if it doesn't exist
    CREATE TABLE WorkoutTemplates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        ExercisesJson NVARCHAR(MAX) NOT NULL,
        RecurrencePattern NVARCHAR(20) NOT NULL,
        DaysOfWeek NVARCHAR(20) NULL,
        IntervalDays INT NULL,
        EstimatedDuration INT NULL,
        Category NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        UsageCount INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastUsedAt DATETIME2 NULL,
        CreatedByUserId INT NULL,
        IsCustom BIT NOT NULL DEFAULT 0,
        Rating FLOAT NULL,
        RatingCount INT NOT NULL DEFAULT 0,
        CONSTRAINT FK_WorkoutTemplates_Users FOREIGN KEY (CreatedByUserId)
            REFERENCES Users(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_WorkoutTemplates_IsCustom_IsActive_Category
        ON WorkoutTemplates(IsCustom, IsActive, Category);

    PRINT 'Created WorkoutTemplates table';
END
GO

-- Step 5: Create WorkoutTemplateRatings table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkoutTemplateRatings')
BEGIN
    CREATE TABLE WorkoutTemplateRatings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        WorkoutTemplateId INT NOT NULL,
        UserId INT NOT NULL,
        Rating FLOAT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
        RatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_WorkoutTemplateRatings_Template FOREIGN KEY (WorkoutTemplateId)
            REFERENCES WorkoutTemplates(Id) ON DELETE CASCADE,
        CONSTRAINT FK_WorkoutTemplateRatings_User FOREIGN KEY (UserId)
            REFERENCES Users(Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_WorkoutTemplateRatings_TemplateId_UserId
        ON WorkoutTemplateRatings(WorkoutTemplateId, UserId);

    PRINT 'Created WorkoutTemplateRatings table';
END
ELSE
BEGIN
    PRINT 'WorkoutTemplateRatings table already exists';
END
GO

PRINT '';
PRINT '=================================================';
PRINT 'Phase 3 database migration completed successfully!';
PRINT '=================================================';
GO

-- Display summary of tables
SELECT
    'SharedWorkouts' as TableName,
    COUNT(*) as RecordCount
FROM SharedWorkouts
UNION ALL
SELECT 'SharedWorkoutLikes', COUNT(*) FROM SharedWorkoutLikes
UNION ALL
SELECT 'SharedWorkoutSaves', COUNT(*) FROM SharedWorkoutSaves
UNION ALL
SELECT 'WorkoutTemplates', COUNT(*) FROM WorkoutTemplates
UNION ALL
SELECT 'WorkoutTemplateRatings', COUNT(*) FROM WorkoutTemplateRatings;
GO
