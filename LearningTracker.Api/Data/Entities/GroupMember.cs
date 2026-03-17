namespace LearningTracker.Api.Data.Entities;

public class GroupMember
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public virtual Group Group { get; set; }
    public virtual User User { get; set; }
}
