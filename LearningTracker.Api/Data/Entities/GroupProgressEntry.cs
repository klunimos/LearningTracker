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

    public GroupGoal GroupGoal { get; set; }
    public User User { get; set; }
    public Book Book { get; set; }
    public BookUnit Unit { get; set; }
}
