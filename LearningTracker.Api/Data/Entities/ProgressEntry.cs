namespace LearningTracker.Api.Data.Entities;

public class ProgressEntry
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public int FromUnitId { get; set; }
    public int ToUnitId { get; set; }
    public string Note { get; set; }
    public DateTime ReportedAt { get; set; }

    public Goal Goal { get; set; }
    public User User { get; set; }
    public Book Book { get; set; }
    public BookUnit FromUnit { get; set; }
    public BookUnit ToUnit { get; set; }
}
