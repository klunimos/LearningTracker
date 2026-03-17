namespace LearningTracker.Api.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public bool IsAdmin { get; set; }
    public string GoogleId { get; set; }
    public string ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Goal> Goals { get; set; }
    public virtual ICollection<ProgressEntry> ProgressEntries { get; set; }
    public virtual ICollection<Category> CreatedCategories { get; set; }
    public virtual ICollection<Book> CreatedBooks { get; set; }
    public virtual ICollection<Group> CreatedGroups { get; set; }
    public virtual ICollection<GroupMember> GroupMembers { get; set; }
    public virtual ICollection<GroupGoalMember> GroupGoalMembers { get; set; }
    public virtual ICollection<GroupProgressEntry> GroupProgressEntries { get; set; }
}
