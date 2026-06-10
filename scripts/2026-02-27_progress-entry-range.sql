-- ============================================================
-- Migration: ProgressEntries – support non-contiguous progress ranges
-- Each entry now stores a FROM-TO unit range instead of a single unit.
--
-- NOTE: statements that reference the freshly-added [FromUnitId] column run via
-- EXEC (dynamic SQL). Otherwise SQL Server compiles the whole batch up-front —
-- before the ALTER ADD has executed — and fails with "Invalid column name".
-- ============================================================

USE [LearningTracker];
GO

-- 1. Add FromUnitId column (nullable first so we can backfill)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ProgressEntries') AND name = 'FromUnitId'
)
BEGIN
    ALTER TABLE [dbo].[ProgressEntries]
        ADD [FromUnitId] INT NULL;

    -- Backfill: existing single-unit entries become a range of one unit
    EXEC(N'UPDATE [dbo].[ProgressEntries] SET [FromUnitId] = [UnitId] WHERE [FromUnitId] IS NULL;');

    -- Now enforce NOT NULL
    EXEC(N'ALTER TABLE [dbo].[ProgressEntries] ALTER COLUMN [FromUnitId] INT NOT NULL;');

    -- Add FK for FromUnitId
    EXEC(N'ALTER TABLE [dbo].[ProgressEntries]
        ADD CONSTRAINT [FK_ProgressEntries_BookUnits_From]
        FOREIGN KEY ([FromUnitId]) REFERENCES [dbo].[BookUnits]([Id]);');
END
GO

-- 2. Rename UnitId -> ToUnitId (only if not yet renamed)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ProgressEntries') AND name = 'UnitId'
)
BEGIN
    EXEC sp_rename 'dbo.ProgressEntries.UnitId', 'ToUnitId', 'COLUMN';
END
GO
