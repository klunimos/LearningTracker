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
