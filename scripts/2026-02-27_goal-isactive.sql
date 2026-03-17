-- ============================================================
-- Migration: Goals – add IsActive column
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Goals') AND name = 'IsActive'
)
BEGIN
    ALTER TABLE [dbo].[Goals]
        ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Goals_IsActive] DEFAULT (1);
END
