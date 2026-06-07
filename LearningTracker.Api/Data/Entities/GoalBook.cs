namespace LearningTracker.Api.Data.Entities;

public class GoalBook
{
    public int GoalId { get; set; }
    public int BookId { get; set; }

    public Goal Goal { get; set; }
    public Book Book { get; set; }
}
