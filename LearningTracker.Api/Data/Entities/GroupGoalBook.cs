namespace LearningTracker.Api.Data.Entities;

public class GroupGoalBook
{
    public int GroupGoalId { get; set; }
    public int BookId { get; set; }

    public virtual GroupGoal GroupGoal { get; set; }
    public virtual Book Book { get; set; }
}
