namespace LearningTracker.Api.Data.Entities;

public class GroupProgressEntry
{
    public int Id { get; set; }
    public int GroupGoalId { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public int UnitId { get; set; }
    public bool IsCollectiveTarget { get; set; }
    public DateTime ReportedAt { get; set; }

    public virtual GroupGoal GroupGoal { get; set; }
    public virtual User User { get; set; }
    public virtual Book Book { get; set; }
    public virtual BookUnit Unit { get; set; }
}
