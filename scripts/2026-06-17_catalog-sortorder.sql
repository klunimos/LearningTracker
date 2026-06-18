-- scripts/2026-06-17_catalog-sortorder.sql
-- Target: LearningTracker
-- Adds SortOrder to Categories and Books so the content catalog can be
-- presented in a curated order (e.g. Shas order for the Talmud) instead of
-- alphabetically by name. Must run BEFORE the Talmud seed (which sets values).
-- Idempotent.

USE [LearningTracker];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Categories') AND name = 'SortOrder')
BEGIN
    ALTER TABLE dbo.Categories
        ADD [SortOrder] INT NOT NULL CONSTRAINT [DF_Categories_SortOrder] DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Books') AND name = 'SortOrder')
BEGIN
    ALTER TABLE dbo.Books
        ADD [SortOrder] INT NOT NULL CONSTRAINT [DF_Books_SortOrder] DEFAULT 0;
END
GO

-- ROLLBACK:
-- ALTER TABLE dbo.Books       DROP CONSTRAINT [DF_Books_SortOrder];
-- ALTER TABLE dbo.Books       DROP COLUMN [SortOrder];
-- ALTER TABLE dbo.Categories  DROP CONSTRAINT [DF_Categories_SortOrder];
-- ALTER TABLE dbo.Categories  DROP COLUMN [SortOrder];
