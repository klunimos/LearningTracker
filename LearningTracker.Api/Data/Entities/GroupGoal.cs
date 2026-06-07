namespace LearningTracker.Api.Data.Entities;

public class GroupGoal
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int? CategoryId { get; set; }
    public string Title { get; set; }
    public DateOnly? TargetDate { get; set; }
    public int? CollectiveTargetUnitId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Group Group { get; set; }
    public Category Category { get; set; }
    public BookUnit CollectiveTargetUnit { get; set; }
    public User CreatedBy { get; set; }
    public ICollection<GroupGoalBook> GroupGoalBooks { get; set; }
    public ICollection<GroupGoalMember> GroupGoalMembers { get; set; }
    public ICollection<GroupProgressEntry> ProgressEntries { get; set; }
}
