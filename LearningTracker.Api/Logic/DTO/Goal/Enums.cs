namespace LearningTracker.Api.Logic.DTO.Goal;

public enum CreateGoalStatus
{
    Success,
    NoBooksSpecified,
    BookNotFound,
    CategoryNotFound,
    StartUnitNotFound
}

public enum SetActiveStatus
{
    Success,
    GoalNotFound
}

public enum ReportProgressStatus
{
    Success,
    GoalNotFound,
    BookNotInGoal,
    UnitNotInBook,
    NoUnitsSpecified,
    UnitsAlreadyReported
}
