namespace LearningTracker.Api.Logic.DTO.GroupGoal;

public class GroupGoalSummaryResponse
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Title { get; set; }
    public bool IsCategoryGoal { get; set; }
    public string ScopeName { get; set; }
    public DateOnly? TargetDate { get; set; }
    public int MemberCount { get; set; }
    public bool IsParticipating { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupGoalDetailResponse
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Title { get; set; }
    public bool IsCategoryGoal { get; set; }
    public string ScopeName { get; set; }
    public DateOnly? TargetDate { get; set; }
    public bool IsParticipating { get; set; }
    public List<MemberProgressResponse> MembersProgress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MemberProgressResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public int BookId { get; set; }
    public string BookName { get; set; }
    public string CurrentUnitName { get; set; }
    public string ExpectedUnitName { get; set; }
    public double ProgressPercent { get; set; }
}

public class GroupGoalHomeItemResponse
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string Title { get; set; }
    public bool IsCategoryGoal { get; set; }
    public string ScopeName { get; set; }
    public DateOnly? TargetDate { get; set; }
    public double ProgressPercent { get; set; }
    public string CurrentUnitName { get; set; }
    public string ExpectedUnitName { get; set; }
    public int? UnitsDelta { get; set; }
    public DateTime CreatedAt { get; set; }
}
