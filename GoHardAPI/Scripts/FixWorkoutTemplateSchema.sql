USE TrainingAppDb;
GO

-- Fix WorkoutTemplate schema to match the new model
PRINT 'Fixing WorkoutTemplate schema...';

-- Add missing columns with default values
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'ExercisesJson')
BEGIN
    ALTER TABLE WorkoutTemplates ADD ExercisesJson NVARCHAR(MAX) NOT NULL DEFAULT '[]';
    PRINT 'Added ExercisesJson column with default';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'Category')
BEGIN
    ALTER TABLE WorkoutTemplates ADD Category NVARCHAR(50) NULL;
    PRINT 'Added Category column';
END

-- Drop UserId column if it exists (we use CreatedByUserId instead)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'UserId')
BEGIN
    -- First, copy data from UserId to CreatedByUserId if needed
    UPDATE WorkoutTemplates
    SET CreatedByUserId = UserId
    WHERE CreatedByUserId IS NULL AND UserId IS NOT NULL;

    -- Drop foreign key constraint if exists
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkoutTemplates_Users_UserId')
    BEGIN
        ALTER TABLE WorkoutTemplates DROP CONSTRAINT FK_WorkoutTemplates_Users_UserId;
        PRINT 'Dropped FK_WorkoutTemplates_Users_UserId';
    END

    -- Drop index if exists
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutTemplates_UserId_IsFavorite')
    BEGIN
        DROP INDEX IX_WorkoutTemplates_UserId_IsFavorite ON WorkoutTemplates;
        PRINT 'Dropped IX_WorkoutTemplates_UserId_IsFavorite';
    END

    ALTER TABLE WorkoutTemplates DROP COLUMN UserId;
    PRINT 'Dropped UserId column (using CreatedByUserId instead)';
END

-- Drop TimesUsed if it exists (we use UsageCount instead)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'TimesUsed')
BEGIN
    -- Copy data to UsageCount
    UPDATE WorkoutTemplates
    SET UsageCount = TimesUsed
    WHERE TimesUsed IS NOT NULL;

    ALTER TABLE WorkoutTemplates DROP COLUMN TimesUsed;
    PRINT 'Dropped TimesUsed column (using UsageCount instead)';
END

-- Drop IsFavorite if it exists (not in new model)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('WorkoutTemplates') AND name = 'IsFavorite')
BEGIN
    ALTER TABLE WorkoutTemplates DROP COLUMN IsFavorite;
    PRINT 'Dropped IsFavorite column';
END

PRINT '';
PRINT '✅ WorkoutTemplate schema fixed!';
PRINT '';

-- Show current schema
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WorkoutTemplates'
ORDER BY ORDINAL_POSITION;

PRINT '';
SELECT 'Total templates: ' + CAST(COUNT(*) AS VARCHAR) FROM WorkoutTemplates;
GO
