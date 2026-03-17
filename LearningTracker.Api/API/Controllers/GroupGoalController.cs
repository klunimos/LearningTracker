using LearningTracker.Api.Logic.DTO.GroupGoal;
using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

[Authorize]
public class GroupGoalController : GlobalController
{
    private readonly IGroupGoalService groupGoalService;

    public GroupGoalController(IGroupGoalService groupGoalService)
    {
        this.groupGoalService = groupGoalService;
    }

    public async Task<IActionResult> GetByGroup(int groupId)
    {
        if (groupId <= 0)
            return Fail("מזהה קבוצה לא תקין");

        var result = await groupGoalService.GetByGroupAsync(UserId, groupId);
        return Success(result);
    }

    public async Task<IActionResult> GetMyParticipatingGoals()
    {
        var result = await groupGoalService.GetMyParticipatingGoalsAsync(UserId);
        return Success(result);
    }

    public async Task<IActionResult> Create(CreateGroupGoalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Fail("כותרת היעד הקבוצתי היא שדה חובה");

        var (response, status) = await groupGoalService.CreateGroupGoalAsync(UserId, request);

        return status switch
        {
            CreateGroupGoalStatus.Success          => Success(response),
            CreateGroupGoalStatus.GroupNotFound    => Fail("הקבוצה לא נמצאה"),
            CreateGroupGoalStatus.NotGroupAdmin    => Fail("רק מנהל קבוצה יכול ליצור יעד קבוצתי"),
            CreateGroupGoalStatus.NoBooksSpecified => Fail("יש לבחור לפחות ספר אחד או קטגוריה"),
            CreateGroupGoalStatus.BookNotFound     => Fail("אחד הספרים שנבחרו לא נמצא"),
            CreateGroupGoalStatus.CategoryNotFound => Fail("הקטגוריה שנבחרה לא נמצאה"),
            _                                      => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> JoinGoal(JoinGroupGoalRequest request)
    {
        if (request.GroupGoalId <= 0)
            return Fail("מזהה יעד לא תקין");

        var (response, status) = await groupGoalService.JoinGroupGoalAsync(UserId, request);

        return status switch
        {
            JoinGroupGoalStatus.Success            => Success(response),
            JoinGroupGoalStatus.GoalNotFound       => Fail("היעד הקבוצתי לא נמצא"),
            JoinGroupGoalStatus.NotGroupMember     => Fail("אינך חבר בקבוצה זו"),
            JoinGroupGoalStatus.AlreadyParticipating => Fail("אתה כבר משתתף ביעד זה"),
            _                                      => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> ReportProgress(ReportGroupProgressRequest request)
    {
        if (request.GroupGoalId <= 0 || request.BookId <= 0 || request.UnitId <= 0)
            return Fail("נתוני דיווח לא תקינים");

        var (response, status) = await groupGoalService.ReportProgressAsync(UserId, request);

        return status switch
        {
            ReportGroupProgressStatus.Success         => Success(response),
            ReportGroupProgressStatus.GoalNotFound    => Fail("היעד הקבוצתי לא נמצא"),
            ReportGroupProgressStatus.NotParticipating => Fail("אינך משתתף ביעד זה"),
            ReportGroupProgressStatus.BookNotInGoal   => Fail("הספר אינו חלק מיעד זה"),
            ReportGroupProgressStatus.UnitNotInBook   => Fail("היחידה אינה שייכת לספר זה"),
            _                                         => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> GetMembersProgress(int groupGoalId)
    {
        if (groupGoalId <= 0)
            return Fail("מזהה יעד לא תקין");

        var (result, found) = await groupGoalService.GetMembersProgressAsync(UserId, groupGoalId);
        if (!found)
            return Fail("היעד הקבוצתי לא נמצא או שאינך חבר בקבוצה");

        return Success(result);
    }
}
