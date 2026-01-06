-- Phase 3: Database Setup for Shared Workouts and Templates
-- Run this script to set up the database tables for Phase 3 features

USE TrainingAppDb;
GO

-- Drop existing tables if they exist (in correct order to handle foreign keys)
IF OBJECT_ID('SharedWorkoutLikes', 'U') IS NOT NULL
    DROP TABLE SharedWorkoutLikes;
GO

IF OBJECT_ID('SharedWorkoutSaves', 'U') IS NOT NULL
    DROP TABLE SharedWorkoutSaves;
GO

IF OBJECT_ID('SharedWorkouts', 'U') IS NOT NULL
    DROP TABLE SharedWorkouts;
GO

IF OBJECT_ID('WorkoutTemplateRatings', 'U') IS NOT NULL
    DROP TABLE WorkoutTemplateRatings;
GO

IF OBJECT_ID('WorkoutTemplates', 'U') IS NOT NULL
    DROP TABLE WorkoutTemplates;
GO

-- Create SharedWorkouts table
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
GO

-- Create indexes for SharedWorkouts
CREATE INDEX IX_SharedWorkouts_Category_SharedAt ON SharedWorkouts(Category, SharedAt);
GO

-- Create SharedWorkoutLikes table
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
GO

-- Create unique index for SharedWorkoutLikes
CREATE UNIQUE INDEX IX_SharedWorkoutLikes_SharedWorkoutId_UserId
    ON SharedWorkoutLikes(SharedWorkoutId, UserId);
GO

-- Create SharedWorkoutSaves table
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
GO

-- Create unique index for SharedWorkoutSaves
CREATE UNIQUE INDEX IX_SharedWorkoutSaves_SharedWorkoutId_UserId
    ON SharedWorkoutSaves(SharedWorkoutId, UserId);
GO

-- Create WorkoutTemplates table
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
GO

-- Create indexes for WorkoutTemplates
CREATE INDEX IX_WorkoutTemplates_IsCustom_IsActive_Category
    ON WorkoutTemplates(IsCustom, IsActive, Category);
GO

-- Create WorkoutTemplateRatings table
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
GO

-- Create unique index for WorkoutTemplateRatings
CREATE UNIQUE INDEX IX_WorkoutTemplateRatings_TemplateId_UserId
    ON WorkoutTemplateRatings(WorkoutTemplateId, UserId);
GO

PRINT 'Phase 3 database setup completed successfully!';
GO
