using LearningTracker.Api.Logic.DTO.User;
using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

[Authorize]
public class UsersController : GlobalController
{
    private readonly IUserService userService;

    public UsersController(IUserService userService)
    {
        this.userService = userService;
    }

    public async Task<IActionResult> Me()
    {
        var result = await userService.GetMeAsync(UserId);
        if (result == null)
            return Fail("משתמש לא נמצא");
        return Success(result);
    }

    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var result = await userService.UpdateProfileAsync(UserId, request.FullName, request.ProfilePicture);
        if (result == null)
            return Fail("משתמש לא נמצא");
        return Success(result);
    }
}
