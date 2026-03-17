-- scripts/2026-02-26_d1-core-schema.sql
-- Target: LearningTracker
-- D1: Core tables — Users, Categories, Books, BookUnits, Goals, GoalBooks, ProgressEntries

USE [LearningTracker];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- Users
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]             INT            IDENTITY(1,1) NOT NULL,
        [Email]          NVARCHAR(256)  NOT NULL,
        [PasswordHash]   NVARCHAR(512)  NULL,
        [FullName]       NVARCHAR(256)  NOT NULL,
        [IsAdmin]        BIT            NOT NULL DEFAULT 0,
        [GoogleId]       NVARCHAR(256)  NULL,
        [ProfilePicture] NVARCHAR(1024) NULL,
        [CreatedAt]      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Users_Email]    ON [dbo].[Users]([Email]);
    CREATE NONCLUSTERED INDEX        [IX_Users_GoogleId] ON [dbo].[Users]([GoogleId]) WHERE [GoogleId] IS NOT NULL;
END
GO

-- ============================================================
-- Categories
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Id]              INT           IDENTITY(1,1) NOT NULL,
        [Name]            NVARCHAR(256) NOT NULL,
        [L1Name]          NVARCHAR(128) NOT NULL,
        [L2Name]          NVARCHAR(128) NOT NULL,
        [UnitName]        NVARCHAR(128) NOT NULL,
        [CreatedByUserId] INT           NOT NULL,
        [CreatedAt]       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Categories]       PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categories_Users] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Categories_CreatedByUserId] ON [dbo].[Categories]([CreatedByUserId]);
END
GO

-- ============================================================
-- Books
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Books' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Books] (
        [Id]              INT           IDENTITY(1,1) NOT NULL,
        [CategoryId]      INT           NOT NULL,
        [Name]            NVARCHAR(256) NOT NULL,
        [SeriesName]      NVARCHAR(256) NULL,
        [CreatedByUserId] INT           NOT NULL,
        [CreatedAt]       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Books]            PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Books_Categories] FOREIGN KEY ([CategoryId])      REFERENCES [dbo].[Categories]([Id]),
        CONSTRAINT [FK_Books_Users]      FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Books_CategoryId]      ON [dbo].[Books]([CategoryId]);
    CREATE NONCLUSTERED INDEX [IX_Books_CreatedByUserId] ON [dbo].[Books]([CreatedByUserId]);
END
GO

-- ============================================================
-- BookUnits
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BookUnits' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[BookUnits] (
        [Id]          INT           IDENTITY(1,1) NOT NULL,
        [BookId]      INT           NOT NULL,
        [L1Label]     NVARCHAR(128) NOT NULL,
        [L1Order]     INT           NOT NULL,
        [UnitLabel]   NVARCHAR(128) NOT NULL,
        [UnitOrder]   INT           NOT NULL,
        [DisplayName] NVARCHAR(256) NOT NULL,
        [SortOrder]   INT           NOT NULL,
        CONSTRAINT [PK_BookUnits]       PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookUnits_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_BookUnits_BookId_SortOrder] ON [dbo].[BookUnits]([BookId], [SortOrder]);
END
GO

-- ============================================================
-- Goals
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Goals' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Goals] (
        [Id]          INT            IDENTITY(1,1) NOT NULL,
        [UserId]      INT            NOT NULL,
        [CategoryId]  INT            NULL,
        [Title]       NVARCHAR(512)  NOT NULL,
        [StartUnitId] INT            NULL,
        [TargetDate]  DATE           NULL,
        [DailyPace]   DECIMAL(10, 2) NULL,
        [IsCompleted] BIT            NOT NULL DEFAULT 0,
        [CreatedAt]   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Goals]                 PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Goals_Users]           FOREIGN KEY ([UserId])      REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Goals_Categories]      FOREIGN KEY ([CategoryId])  REFERENCES [dbo].[Categories]([Id]),
        CONSTRAINT [FK_Goals_BookUnits_Start] FOREIGN KEY ([StartUnitId]) REFERENCES [dbo].[BookUnits]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Goals_UserId]     ON [dbo].[Goals]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_Goals_CategoryId] ON [dbo].[Goals]([CategoryId]) WHERE [CategoryId] IS NOT NULL;
END
GO

-- ============================================================
-- GoalBooks  (junction: Goal ↔ Book)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GoalBooks' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[GoalBooks] (
        [GoalId] INT NOT NULL,
        [BookId] INT NOT NULL,
        CONSTRAINT [PK_GoalBooks]       PRIMARY KEY ([GoalId], [BookId]),
        CONSTRAINT [FK_GoalBooks_Goals] FOREIGN KEY ([GoalId]) REFERENCES [dbo].[Goals]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GoalBooks_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_GoalBooks_GoalId] ON [dbo].[GoalBooks]([GoalId]);
    CREATE NONCLUSTERED INDEX [IX_GoalBooks_BookId] ON [dbo].[GoalBooks]([BookId]);
END
GO

-- ============================================================
-- ProgressEntries
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProgressEntries' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ProgressEntries] (
        [Id]         INT            IDENTITY(1,1) NOT NULL,
        [GoalId]     INT            NOT NULL,
        [UserId]     INT            NOT NULL,
        [BookId]     INT            NOT NULL,
        [UnitId]     INT            NOT NULL,
        [Note]       NVARCHAR(1024) NULL,
        [ReportedAt] DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProgressEntries]         PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProgressEntries_Goals]     FOREIGN KEY ([GoalId])  REFERENCES [dbo].[Goals]([Id])     ON DELETE CASCADE,
        CONSTRAINT [FK_ProgressEntries_Users]     FOREIGN KEY ([UserId])  REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_ProgressEntries_Books]     FOREIGN KEY ([BookId])  REFERENCES [dbo].[Books]([Id]),
        CONSTRAINT [FK_ProgressEntries_BookUnits] FOREIGN KEY ([UnitId])  REFERENCES [dbo].[BookUnits]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_ProgressEntries_GoalId]              ON [dbo].[ProgressEntries]([GoalId]);
    CREATE NONCLUSTERED INDEX [IX_ProgressEntries_UserId]              ON [dbo].[ProgressEntries]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_ProgressEntries_BookId]              ON [dbo].[ProgressEntries]([BookId]);
    -- Optimizes "current position" query: latest entry per (GoalId, BookId)
    CREATE NONCLUSTERED INDEX [IX_ProgressEntries_GoalId_BookId_Date]  ON [dbo].[ProgressEntries]([GoalId], [BookId], [ReportedAt] DESC);
END
GO

-- ============================================================
-- ROLLBACK:
-- DROP TABLE IF EXISTS [dbo].[ProgressEntries];
-- DROP TABLE IF EXISTS [dbo].[GoalBooks];
-- DROP TABLE IF EXISTS [dbo].[Goals];
-- DROP TABLE IF EXISTS [dbo].[BookUnits];
-- DROP TABLE IF EXISTS [dbo].[Books];
-- DROP TABLE IF EXISTS [dbo].[Categories];
-- DROP TABLE IF EXISTS [dbo].[Users];
-- ============================================================
