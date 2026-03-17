namespace LearningTracker.Api.Data.Entities;

public class Book
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string SeriesName { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; }
    public virtual User CreatedBy { get; set; }
    public virtual ICollection<BookUnit> Units { get; set; }
    public virtual ICollection<GoalBook> GoalBooks { get; set; }
    public virtual ICollection<ProgressEntry> ProgressEntries { get; set; }
    public virtual ICollection<GroupGoalBook> GroupGoalBooks { get; set; }
    public virtual ICollection<GroupProgressEntry> GroupProgressEntries { get; set; }
}
