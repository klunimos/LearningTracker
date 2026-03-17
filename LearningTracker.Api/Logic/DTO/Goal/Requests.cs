#nullable enable
using System.ComponentModel.DataAnnotations;

namespace LearningTracker.Api.Logic.DTO.Goal;

public class CreateGoalRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Title { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public List<int> BookIds { get; set; } = new();
    public int? StartUnitId { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal? DailyPace { get; set; }
}

public class SetActiveRequest
{
    public int GoalId { get; set; }
    public bool IsActive { get; set; }
}

public class ReportProgressRequest
{
    public int GoalId { get; set; }
    public int BookId { get; set; }
    /// <summary>
    /// IDs of units the user completed. May be non-contiguous; the service
    /// groups them into contiguous ranges and creates one ProgressEntry per range.
    /// </summary>
    public List<int> UnitIds { get; set; } = new();
    public string? Note { get; set; }
}
