-- scripts/2026-02-26_d2-groups-notifications-schema.sql
-- Target: LearningTracker
-- D2: Groups tables — Groups, GroupMembers, GroupGoals, GroupGoalBooks,
--                     GroupGoalMembers, GroupProgressEntries, Notifications

USE [LearningTracker];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- Groups
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Groups' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Groups] (
        [Id]              INT           IDENTITY(1,1) NOT NULL,
        [Name]            NVARCHAR(256) NOT NULL,
        [Description]     NVARCHAR(1024) NULL,
        [ProfilePicture]  NVARCHAR(MAX) NULL,
        [InviteCode]      NVARCHAR(32)  NOT NULL,
        [IsPublic]        BIT           NOT NULL DEFAULT 0,
        [CreatedByUserId] INT           NOT NULL,
        [CreatedAt]       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Groups]       PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Groups_Users] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users]([Id])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Groups_InviteCode]      ON [dbo].[Groups]([InviteCode]);
    CREATE NONCLUSTERED INDEX        [IX_Groups_CreatedByUserId] ON [dbo].[Groups]([CreatedByUserId]);
    CREATE NONCLUSTERED INDEX        [IX_Groups_IsPublic]        ON [dbo].[Groups]([IsPublic]);
END
GO

-- ============================================================
-- GroupMembers  (junction: Group ↔ User, with Role)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GroupMembers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[GroupMembers] (
        [GroupId]  INT          NOT NULL,
        [UserId]   INT          NOT NULL,
        [Role]     NVARCHAR(64) NOT NULL DEFAULT 'Member',
        [JoinedAt] DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_GroupMembers]        PRIMARY KEY ([GroupId], [UserId]),
        CONSTRAINT [FK_GroupMembers_Groups] FOREIGN KEY ([GroupId]) REFERENCES [dbo].[Groups]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupMembers_Users]  FOREIGN KEY ([UserId])  REFERENCES [dbo].[Users]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_GroupMembers_UserId] ON [dbo].[GroupMembers]([UserId]);
END
GO

-- ============================================================
-- GroupGoals
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GroupGoals' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[GroupGoals] (
        [Id]                    INT           IDENTITY(1,1) NOT NULL,
        [GroupId]               INT           NOT NULL,
        [CategoryId]            INT           NULL,
        [Title]                 NVARCHAR(512) NOT NULL,
        [TargetDate]            DATE          NULL,
        [CollectiveTargetUnitId] INT          NULL,
        [CreatedByUserId]       INT           NOT NULL,
        [CreatedAt]             DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_GroupGoals]                    PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GroupGoals_Groups]             FOREIGN KEY ([GroupId])               REFERENCES [dbo].[Groups]([Id])     ON DELETE CASCADE,
        CONSTRAINT [FK_GroupGoals_Categories]         FOREIGN KEY ([CategoryId])            REFERENCES [dbo].[Categories]([Id]),
        CONSTRAINT [FK_GroupGoals_BookUnits_Target]   FOREIGN KEY ([CollectiveTargetUnitId]) REFERENCES [dbo].[BookUnits]([Id]),
        CONSTRAINT [FK_GroupGoals_Users]              FOREIGN KEY ([CreatedByUserId])        REFERENCES [dbo].[Users]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_GroupGoals_GroupId]    ON [dbo].[GroupGoals]([GroupId]);
    CREATE NONCLUSTERED INDEX [IX_GroupGoals_CategoryId] ON [dbo].[GroupGoals]([CategoryId]) WHERE [CategoryId] IS NOT NULL;
END
GO

-- ============================================================
-- GroupGoalBooks  (junction: GroupGoal ↔ Book)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GroupGoalBooks' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[GroupGoalBooks] (
        [GroupGoalId] INT NOT NULL,
        [BookId]      INT NOT NULL,
        CONSTRAINT [PK_GroupGoalBooks]            PRIMARY KEY ([GroupGoalId], [BookId]),
        CONSTRAINT [FK_GroupGoalBooks_GroupGoals] FOREIGN KEY ([GroupGoalId]) REFERENCES [dbo].[GroupGoals]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupGoalBooks_Books]      FOREIGN KEY ([BookId])      REFERENCES [dbo].[Books]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_GroupGoalBooks_GroupGoalId] ON [dbo].[GroupGoalBooks]([GroupGoalId]);
    CREATE NONCLUSTERED INDEX [IX_GroupGoalBooks_BookId]      ON [dbo].[GroupGoalBooks]([BookId]);
END
GO

-- ============================================================
-- GroupGoalMembers  (active join: member opts into a group goal)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GroupGoalMembers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[GroupGoalMembers] (
        [GroupGoalId] INT       NOT NULL,
        [UserId]      INT       NOT NULL,
        [JoinedAt]    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_GroupGoalMembers]            PRIMARY KEY ([GroupGoalId], [UserId]),
        CONSTRAINT [FK_GroupGoalMembers_GroupGoals] FOREIGN KEY ([GroupGoalId]) REFERENCES [dbo].[GroupGoals]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupGoalMembers_Users]      FOREIGN KEY ([UserId])      REFERENCES [dbo].[Users]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_GroupGoalMembers_GroupGoalId] ON [dbo].[GroupGoalMembers]([GroupGoalId]);
    CREATE NONCLUSTERED INDEX [IX_GroupGoalMembers_UserId]      ON [dbo].[GroupGoalMembers]([UserId]);
END
GO

-- ============================================================
-- GroupProgressEntries
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GroupProgressEntries' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[GroupProgressEntries] (
        [Id]                  INT       IDENTITY(1,1) NOT NULL,
        [GroupGoalId]         INT       NOT NULL,
        [UserId]              INT       NOT NULL,
        [BookId]              INT       NOT NULL,
        [UnitId]              INT       NOT NULL,
        [IsCollectiveTarget]  BIT       NOT NULL DEFAULT 0,
        [ReportedAt]          DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_GroupProgressEntries]              PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GroupProgressEntries_GroupGoals]   FOREIGN KEY ([GroupGoalId]) REFERENCES [dbo].[GroupGoals]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupProgressEntries_Users]        FOREIGN KEY ([UserId])      REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_GroupProgressEntries_Books]        FOREIGN KEY ([BookId])      REFERENCES [dbo].[Books]([Id]),
        CONSTRAINT [FK_GroupProgressEntries_BookUnits]    FOREIGN KEY ([UnitId])      REFERENCES [dbo].[BookUnits]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_GroupProgressEntries_GroupGoalId]             ON [dbo].[GroupProgressEntries]([GroupGoalId]);
    CREATE NONCLUSTERED INDEX [IX_GroupProgressEntries_UserId]                  ON [dbo].[GroupProgressEntries]([UserId]);
    -- Optimizes "current position per member per book" query
    CREATE NONCLUSTERED INDEX [IX_GroupProgressEntries_GoalId_UserId_BookId_Date] ON [dbo].[GroupProgressEntries]([GroupGoalId], [UserId], [BookId], [ReportedAt] DESC);
END
GO

-- ============================================================
-- Notifications
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [UserId]              INT            NOT NULL,
        [Message]             NVARCHAR(1024) NOT NULL,
        [Type]                NVARCHAR(64)   NOT NULL,
        [IsRead]              BIT            NOT NULL DEFAULT 0,
        [RelatedEntityType]   NVARCHAR(64)   NULL,
        [RelatedEntityId]     INT            NULL,
        [CreatedAt]           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Notifications]       PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId_IsRead]  ON [dbo].[Notifications]([UserId], [IsRead]);
    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId_Created] ON [dbo].[Notifications]([UserId], [CreatedAt] DESC);
END
GO

-- ============================================================
-- ROLLBACK:
-- DROP TABLE IF EXISTS [dbo].[Notifications];
-- DROP TABLE IF EXISTS [dbo].[GroupProgressEntries];
-- DROP TABLE IF EXISTS [dbo].[GroupGoalMembers];
-- DROP TABLE IF EXISTS [dbo].[GroupGoalBooks];
-- DROP TABLE IF EXISTS [dbo].[GroupGoals];
-- DROP TABLE IF EXISTS [dbo].[GroupMembers];
-- DROP TABLE IF EXISTS [dbo].[Groups];
-- ============================================================
