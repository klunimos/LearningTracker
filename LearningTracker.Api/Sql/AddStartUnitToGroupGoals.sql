-- Adds the personal-style start unit to group goals (the donut measures from here
-- to the end of the book — the "final target"). The existing CollectiveTargetUnitId
-- stays as the group's current collective holding position.
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'GroupGoals' AND COLUMN_NAME = 'StartUnitId')
BEGIN
    ALTER TABLE dbo.GroupGoals ADD StartUnitId INT NULL;

    ALTER TABLE dbo.GroupGoals
        ADD CONSTRAINT FK_GroupGoals_StartUnit
        FOREIGN KEY (StartUnitId) REFERENCES dbo.BookUnits(Id);
END;
