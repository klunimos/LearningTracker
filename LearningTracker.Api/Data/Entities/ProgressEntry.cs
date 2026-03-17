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

    public virtual Goal Goal { get; set; }
    public virtual User User { get; set; }
    public virtual Book Book { get; set; }
    public virtual BookUnit FromUnit { get; set; }
    public virtual BookUnit ToUnit { get; set; }
}
