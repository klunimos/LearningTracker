using LearningTracker.Api.Logic.DTO.Group;
using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

[Authorize]
public class GroupController : GlobalController
{
    private readonly IGroupService groupService;

    public GroupController(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public async Task<IActionResult> GetMine()
    {
        var result = await groupService.GetMyGroupsAsync(UserId);
        return Success(result);
    }

    public async Task<IActionResult> Create(CreateGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Fail("שם הקבוצה הוא שדה חובה");
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            return Fail("קוד הצטרפות הוא שדה חובה");

        var (response, status) = await groupService.CreateGroupAsync(UserId, request);

        return status switch
        {
            CreateGroupStatus.Success               => Success(response),
            CreateGroupStatus.InvalidInviteCode    => Fail("קוד ההצטרפות חייב להכיל לפחות 8 ספרות"),
            CreateGroupStatus.InviteCodeAlreadyExists => Fail("קוד ההצטרפות כבר קיים. בחר קוד אחר"),
            _                                       => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> Join(JoinGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            return Fail("קוד הצטרפות הוא שדה חובה");

        var (response, status) = await groupService.JoinGroupAsync(UserId, request);

        return status switch
        {
            JoinGroupStatus.Success      => Success(response),
            JoinGroupStatus.NotFound     => Fail("קוד ההצטרפות לא נמצא"),
            JoinGroupStatus.AlreadyMember => Fail("אתה כבר חבר בקבוצה זו"),
            _                            => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> GetDetail(int groupId)
    {
        if (groupId <= 0)
            return Fail("מזהה קבוצה לא תקין");

        var (response, found) = await groupService.GetGroupDetailAsync(UserId, groupId);
        if (!found)
            return Fail("הקבוצה לא נמצאה");

        return Success(response);
    }

    public async Task<IActionResult> UpdateSettings(UpdateGroupSettingsRequest request)
    {
        if (request.GroupId <= 0)
            return Fail("מזהה קבוצה לא תקין");
        if (string.IsNullOrWhiteSpace(request.Name))
            return Fail("שם הקבוצה הוא שדה חובה");
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            return Fail("קוד הצטרפות הוא שדה חובה");

        var (response, status) = await groupService.UpdateSettingsAsync(UserId, request);
        return status switch
        {
            UpdateGroupSettingsStatus.Success              => Success(response),
            UpdateGroupSettingsStatus.GroupNotFound        => Fail("הקבוצה לא נמצאה"),
            UpdateGroupSettingsStatus.NotGroupAdmin        => Fail("רק מנהל קבוצה יכול לעדכן הגדרות"),
            UpdateGroupSettingsStatus.InvalidInviteCode    => Fail("קוד ההצטרפות חייב להכיל לפחות 8 ספרות"),
            UpdateGroupSettingsStatus.InviteCodeAlreadyExists => Fail("קוד ההצטרפות כבר קיים. בחר קוד אחר"),
            _                                              => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Fail("יש להזין מילות חיפוש");

        var result = await groupService.SearchGroupsAsync(UserId, query);
        return Success(result);
    }
}
