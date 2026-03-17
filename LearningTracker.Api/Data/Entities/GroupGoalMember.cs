namespace LearningTracker.Api.Data.Entities;

public class GroupGoalMember
{
    public int GroupGoalId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public virtual GroupGoal GroupGoal { get; set; }
    public virtual User User { get; set; }
}
