namespace LearningTracker.Api.Data.Entities;

public class BookUnit
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string L1Label { get; set; }
    public int L1Order { get; set; }
    public string UnitLabel { get; set; }
    public int UnitOrder { get; set; }
    public string DisplayName { get; set; }
    public int SortOrder { get; set; }

    public virtual Book Book { get; set; }
    public virtual ICollection<Goal> GoalsAsStart { get; set; }
    public virtual ICollection<ProgressEntry> ProgressEntriesAsFrom { get; set; }
    public virtual ICollection<ProgressEntry> ProgressEntriesAsTo { get; set; }
    public virtual ICollection<GroupProgressEntry> GroupProgressEntries { get; set; }
    public virtual ICollection<GroupGoal> GroupGoalsAsCollectiveTarget { get; set; }
}
