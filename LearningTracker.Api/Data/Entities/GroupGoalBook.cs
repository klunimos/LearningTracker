namespace LearningTracker.Api.Data.Entities;

public class GroupGoalBook
{
    public int GroupGoalId { get; set; }
    public int BookId { get; set; }

    public GroupGoal GroupGoal { get; set; }
    public Book Book { get; set; }
}
