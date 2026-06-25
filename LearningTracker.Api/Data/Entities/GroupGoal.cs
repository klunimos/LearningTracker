namespace LearningTracker.Api.Data.Entities;

public class GroupGoal
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int? CategoryId { get; set; }
    public string Title { get; set; }
    public DateOnly? TargetDate { get; set; }
    /// <summary>Personal-style start unit — the final target spans from here to the end of the book.</summary>
    public int? StartUnitId { get; set; }
    /// <summary>Where the group collectively holds right now (group-only). Marked on the bottom bar / report modal.</summary>
    public int? CollectiveTargetUnitId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Group Group { get; set; }
    public Category Category { get; set; }
    public BookUnit StartUnit { get; set; }
    public BookUnit CollectiveTargetUnit { get; set; }
    public User CreatedBy { get; set; }
    public ICollection<GroupGoalBook> GroupGoalBooks { get; set; }
    public ICollection<GroupGoalMember> GroupGoalMembers { get; set; }
    public ICollection<GroupProgressEntry> ProgressEntries { get; set; }
}
