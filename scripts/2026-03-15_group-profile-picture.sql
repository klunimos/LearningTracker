-- scripts/2026-03-15_group-profile-picture.sql
-- Target: LearningTracker
-- Add profile picture column to Groups

USE [LearningTracker];
GO

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Groups', 'ProfilePicture') IS NULL
BEGIN
    ALTER TABLE [dbo].[Groups]
    ADD [ProfilePicture] NVARCHAR(MAX) NULL;
END
ELSE
BEGIN
    ALTER TABLE [dbo].[Groups]
    ALTER COLUMN [ProfilePicture] NVARCHAR(MAX) NULL;
END
GO
