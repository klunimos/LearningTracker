namespace LearningTracker.Api.Data.Entities;

public class GroupMember
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public Group Group { get; set; }
    public User User { get; set; }
}
