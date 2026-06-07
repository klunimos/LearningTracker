namespace LearningTracker.Api.Data.Entities;

public class Goal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    public string Title { get; set; }
    public int? StartUnitId { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal? DailyPace { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; }
    public Category Category { get; set; }
    public BookUnit StartUnit { get; set; }
    public ICollection<GoalBook> GoalBooks { get; set; }
    public ICollection<ProgressEntry> ProgressEntries { get; set; }
}
