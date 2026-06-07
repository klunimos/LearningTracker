namespace LearningTracker.Api.Data.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProfilePicture { get; set; }
    public string InviteCode { get; set; }
    public bool IsPublic { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User CreatedBy { get; set; }
    public ICollection<GroupMember> Members { get; set; }
    public ICollection<GroupGoal> GroupGoals { get; set; }
}
