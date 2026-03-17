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

    public virtual Group Group { get; set; }
    public virtual Category Category { get; set; }
    public virtual BookUnit CollectiveTargetUnit { get; set; }
    public virtual User CreatedBy { get; set; }
    public virtual ICollection<GroupGoalBook> GroupGoalBooks { get; set; }
    public virtual ICollection<GroupGoalMember> GroupGoalMembers { get; set; }
    public virtual ICollection<GroupProgressEntry> ProgressEntries { get; set; }
}
