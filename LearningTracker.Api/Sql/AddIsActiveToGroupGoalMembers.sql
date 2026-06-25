-- Lets a participant make their OWN participation in a group goal inactive
-- (like the active toggle on a personal goal) without affecting the goal for others.
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'GroupGoalMembers' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE dbo.GroupGoalMembers
        ADD IsActive BIT NOT NULL
        CONSTRAINT DF_GroupGoalMembers_IsActive DEFAULT 1;
END;
