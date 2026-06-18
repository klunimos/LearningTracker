namespace LearningTracker.Api.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string L1Name { get; set; }
    public string L2Name { get; set; }
    public string UnitName { get; set; }
    public int SortOrder { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User CreatedBy { get; set; }
    public ICollection<Book> Books { get; set; }
    public ICollection<Goal> Goals { get; set; }
    public ICollection<GroupGoal> GroupGoals { get; set; }
}
