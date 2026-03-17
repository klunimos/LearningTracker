namespace LearningTracker.Api.Data.Entities;

public class GoalBook
{
    public int GoalId { get; set; }
    public int BookId { get; set; }

    public virtual Goal Goal { get; set; }
    public virtual Book Book { get; set; }
}
