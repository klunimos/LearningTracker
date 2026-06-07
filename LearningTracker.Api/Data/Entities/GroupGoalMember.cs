namespace LearningTracker.Api.Data.Entities;

public class GroupGoalMember
{
    public int GroupGoalId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public GroupGoal GroupGoal { get; set; }
    public User User { get; set; }
}
