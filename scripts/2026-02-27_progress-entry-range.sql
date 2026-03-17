-- ============================================================
-- Migration: ProgressEntries – support non-contiguous progress ranges
-- Each entry now stores a FROM-TO unit range instead of a single unit.
-- ============================================================

-- 1. Add FromUnitId column (nullable first so we can backfill)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ProgressEntries') AND name = 'FromUnitId'
)
BEGIN
    ALTER TABLE [dbo].[ProgressEntries]
        ADD [FromUnitId] INT NULL;

    -- Backfill: existing single-unit entries become a range of one unit
    UPDATE [dbo].[ProgressEntries]
        SET [FromUnitId] = [UnitId]
    WHERE [FromUnitId] IS NULL;

    -- Now enforce NOT NULL
    ALTER TABLE [dbo].[ProgressEntries]
        ALTER COLUMN [FromUnitId] INT NOT NULL;

    -- Add FK for FromUnitId
    ALTER TABLE [dbo].[ProgressEntries]
        ADD CONSTRAINT [FK_ProgressEntries_BookUnits_From]
        FOREIGN KEY ([FromUnitId]) REFERENCES [dbo].[BookUnits]([Id]);
END

-- 2. Rename UnitId → ToUnitId (only if not yet renamed)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ProgressEntries') AND name = 'UnitId'
)
BEGIN
    EXEC sp_rename 'dbo.ProgressEntries.UnitId', 'ToUnitId', 'COLUMN';
END
