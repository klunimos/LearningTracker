namespace LearningTracker.Api.Data.Entities;

public class Book
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string SeriesName { get; set; }
    public int SortOrder { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Category Category { get; set; }
    public User CreatedBy { get; set; }
    public ICollection<BookUnit> Units { get; set; }
    public ICollection<GoalBook> GoalBooks { get; set; }
    public ICollection<ProgressEntry> ProgressEntries { get; set; }
    public ICollection<GroupGoalBook> GroupGoalBooks { get; set; }
    public ICollection<GroupProgressEntry> GroupProgressEntries { get; set; }
}
