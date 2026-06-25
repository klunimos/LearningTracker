using System.ComponentModel.DataAnnotations;

namespace LearningTracker.Api.Logic.DTO.GroupGoal;

public class CreateGroupGoalRequest
{
    public int GroupId { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Title { get; set; }

    public int? CategoryId { get; set; }
    public List<int> BookIds { get; set; } = new();
    public DateOnly? TargetDate { get; set; }
    /// <summary>Personal-style start unit — the final target spans from here to the end of the book.</summary>
    public int? StartUnitId { get; set; }
    /// <summary>Where the group collectively holds right now (group-only).</summary>
    public int? CollectiveTargetUnitId { get; set; }
}

public class JoinGroupGoalRequest
{
    public int GroupGoalId { get; set; }
}

public class ReportGroupProgressRequest
{
    public int GroupGoalId { get; set; }
    public int BookId { get; set; }
    public int UnitId { get; set; }
    public bool IsCollectiveTarget { get; set; }
}

public class SetCollectiveTargetRequest
{
    public int GroupGoalId { get; set; }
    /// <summary>The unit where the group now collectively holds, or null to clear it.</summary>
    public int? CollectiveTargetUnitId { get; set; }
}

public class SetParticipationActiveRequest
{
    public int GroupGoalId { get; set; }
    public bool IsActive { get; set; }
}
