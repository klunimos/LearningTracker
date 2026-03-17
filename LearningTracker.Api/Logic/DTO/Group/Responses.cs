namespace LearningTracker.Api.Logic.DTO.Group;

public class GroupSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProfilePicture { get; set; }
    public bool IsPublic { get; set; }
    public string InviteCode { get; set; }
    public int MemberCount { get; set; }
    public int GoalCount { get; set; }
    public string MyRole { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupDetailResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProfilePicture { get; set; }
    public bool IsPublic { get; set; }
    public string InviteCode { get; set; }
    public string MyRole { get; set; }
    public List<GroupMemberResponse> Members { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupMemberResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
