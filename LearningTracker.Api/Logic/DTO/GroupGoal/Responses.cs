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
    public int CurrentUnitId { get; set; }
    public string ExpectedUnitName { get; set; }
    public double ProgressPercent { get; set; }
}

public class GroupGoalBookResponse
{
    public int BookId { get; set; }
    public string BookName { get; set; }
}

public class GroupGoalPageResponse
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string Title { get; set; }
    public bool IsCategoryGoal { get; set; }
    public string ScopeName { get; set; }
    public DateOnly? TargetDate { get; set; }
    public bool IsParticipating { get; set; }
    /// <summary>Whether the viewer's own participation in the goal is active (false = they paused it for themselves).</summary>
    public bool IsActive { get; set; }
    /// <summary>True when the viewer is an admin of the group (may set the temporary/collective target).</summary>
    public bool IsGroupAdmin { get; set; }
    public List<GroupGoalBookResponse> Books { get; set; }
    public List<MemberProgressResponse> MembersProgress { get; set; }
    public DateTime CreatedAt { get; set; }

    // ── My personal progress (drives the donut, identical to a personal goal) ──
    public int TotalUnits { get; set; }
    public int MyCompletedUnits { get; set; }
    public double MyProgressPercent { get; set; }
    public string MyCurrentUnitName { get; set; }

    // ── Donut marker: where the group is *supposed* to hold (pace toward the final target) ──
    /// <summary>Pace-expected position toward the final target (0–100), derived from the target date. Null when no target date.</summary>
    public double? PaceTargetPercent { get; set; }
    /// <summary>The unit the group is expected to be at by now (pace).</summary>
    public string PaceTargetLabel { get; set; }

    // ── Bottom-bar + report-modal marker: where the group collectively holds ──
    /// <summary>The group's collective holding position (0–100), or null when none was set.</summary>
    public double? CollectiveTargetPercent { get; set; }
    /// <summary>The unit where the group collectively holds.</summary>
    public string CollectiveTargetLabel { get; set; }
    /// <summary>The collective-position unit, marked in the report modal. Null when none was set.</summary>
    public int? CollectiveTargetUnitId { get; set; }
    /// <summary>The book the collective-position unit belongs to (so the marker only shows for that book).</summary>
    public int? CollectiveTargetBookId { get; set; }
}

public class JoinableGroupGoalResponse
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string Title { get; set; }
    public bool IsCategoryGoal { get; set; }
    public string ScopeName { get; set; }
    public DateOnly? TargetDate { get; set; }
    public int MemberCount { get; set; }
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
