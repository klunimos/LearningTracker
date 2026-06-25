namespace LearningTracker.Api.Logic.DTO.GroupGoal;

public enum CreateGroupGoalStatus
{
    Success,
    GroupNotFound,
    NotGroupAdmin,
    NoBooksSpecified,
    BookNotFound,
    CategoryNotFound
}

public enum JoinGroupGoalStatus
{
    Success,
    GoalNotFound,
    NotGroupMember,
    AlreadyParticipating
}

public enum ReportGroupProgressStatus
{
    Success,
    GoalNotFound,
    NotParticipating,
    BookNotInGoal,
    UnitNotInBook
}

public enum SetCollectiveTargetStatus
{
    Success,
    GoalNotFound,
    NotGroupAdmin,
    UnitNotInGoal
}

public enum SetParticipationActiveStatus
{
    Success,
    GoalNotFound,
    NotParticipating
}
