using LearningTracker.Api.Logic.DTO.Goal;
using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

[Authorize]
public class GoalController : GlobalController
{
    private readonly IGoalService goalService;

    public GoalController(IGoalService goalService)
    {
        this.goalService = goalService;
    }

    public async Task<IActionResult> GetMine([FromQuery] bool includeInactive = false)
    {
        var result = await goalService.GetMyGoalsAsync(UserId, includeInactive);
        return Success(result);
    }

    public async Task<IActionResult> SetActive(SetActiveRequest request)
    {
        if (request.GoalId <= 0)
            return Fail("נתונים לא תקינים");

        var (response, status) = await goalService.SetActiveAsync(UserId, request);

        return status switch
        {
            SetActiveStatus.Success      => Success(response),
            SetActiveStatus.GoalNotFound => Fail("היעד לא נמצא"),
            _                            => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> Create(CreateGoalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Fail("כותרת היעד היא שדה חובה");

        var (response, status) = await goalService.CreateGoalAsync(UserId, request);

        return status switch
        {
            CreateGoalStatus.Success          => Success(response),
            CreateGoalStatus.NoBooksSpecified => Fail("יש לבחור לפחות ספר אחד או קטגוריה"),
            CreateGoalStatus.BookNotFound     => Fail("אחד הספרים שנבחרו לא נמצא"),
            CreateGoalStatus.CategoryNotFound => Fail("הקטגוריה שנבחרה לא נמצאה"),
            CreateGoalStatus.StartUnitNotFound => Fail("יחידת ההתחלה שנבחרה לא נמצאה"),
            _                                 => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> ReportProgress(ReportProgressRequest request)
    {
        if (request.GoalId <= 0 || request.BookId <= 0)
            return Fail("נתוני דיווח לא תקינים");

        var (response, status) = await goalService.ReportProgressAsync(UserId, request);

        return status switch
        {
            ReportProgressStatus.Success              => Success(response),
            ReportProgressStatus.GoalNotFound         => Fail("היעד לא נמצא"),
            ReportProgressStatus.BookNotInGoal        => Fail("הספר אינו חלק מיעד זה"),
            ReportProgressStatus.UnitNotInBook        => Fail("אחת היחידות שנבחרו אינה שייכת לספר זה"),
            ReportProgressStatus.NoUnitsSpecified     => Fail("יש לבחור לפחות יחידה אחת"),
            ReportProgressStatus.UnitsAlreadyReported => Fail("חלק מהיחידות שנבחרו כבר דווחו קודם לכן"),
            _                                         => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> CalculatePace(int bookId, int startUnitId, DateOnly targetDate)
    {
        if (bookId <= 0 || startUnitId <= 0)
            return Fail("נתוני חישוב לא תקינים");

        var (response, found) = await goalService.CalculatePaceAsync(bookId, startUnitId, targetDate);
        if (!found)
            return Fail("הספר או יחידת ההתחלה לא נמצאו");

        return Success(response);
    }

    public async Task<IActionResult> CalculateTargetDate(int bookId, int startUnitId, decimal dailyPace)
    {
        if (bookId <= 0 || startUnitId <= 0 || dailyPace <= 0)
            return Fail("נתוני חישוב לא תקינים");

        var (response, found) = await goalService.CalculateTargetDateAsync(bookId, startUnitId, dailyPace);
        if (!found)
            return Fail("הספר או יחידת ההתחלה לא נמצאו");

        return Success(response);
    }
}
