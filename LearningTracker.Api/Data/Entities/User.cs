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

    public ICollection<Goal> Goals { get; set; }
    public ICollection<ProgressEntry> ProgressEntries { get; set; }
    public ICollection<Category> CreatedCategories { get; set; }
    public ICollection<Book> CreatedBooks { get; set; }
    public ICollection<Group> CreatedGroups { get; set; }
    public ICollection<GroupMember> GroupMembers { get; set; }
    public ICollection<GroupGoalMember> GroupGoalMembers { get; set; }
    public ICollection<GroupGoal> CreatedGroupGoals { get; set; }
    public ICollection<GroupProgressEntry> GroupProgressEntries { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}
