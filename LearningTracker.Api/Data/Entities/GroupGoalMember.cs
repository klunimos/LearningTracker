namespace LearningTracker.Api.Data.Entities;

public class GroupGoalMember
{
    public int GroupGoalId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    /// <summary>Whether this member's own participation in the goal is active (affects only this member).</summary>
    public bool IsActive { get; set; } = true;

    public GroupGoal GroupGoal { get; set; }
    public User User { get; set; }
}
