namespace LearningTracker.Api.Logic.DTO.Goal;

public class GoalSummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsCategoryGoal { get; set; }
    public string ScopeName { get; set; }
    public int TotalUnits { get; set; }
    public int CompletedUnits { get; set; }
    public double ProgressPercent { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal? DailyPace { get; set; }
    public bool IsOnTrack { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsActive { get; set; }
    public List<BookProgressResponse> Books { get; set; }
}

public class BookProgressResponse
{
    public int BookId { get; set; }
    public string BookName { get; set; }
    public string CurrentUnitName { get; set; }
    public int CurrentUnitId { get; set; }
    public string ExpectedUnitName { get; set; }
    public int? ExpectedUnitId { get; set; }
    public int TotalUnits { get; set; }
    public double ProgressPercent { get; set; }
    /// <summary>All unit IDs covered by reported ranges for this book/goal.</summary>
    public List<int> ReportedUnitIds { get; set; } = new();
}

public class PaceCalcResponse
{
    public decimal RequiredDailyPace { get; set; }
    public int TotalUnitsRemaining { get; set; }
    public int DaysRemaining { get; set; }
}

public class TargetDateCalcResponse
{
    public DateOnly TargetDate { get; set; }
    public int TotalUnitsRemaining { get; set; }
    public int DaysNeeded { get; set; }
}
