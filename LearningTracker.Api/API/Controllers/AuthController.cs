using LearningTracker.Api.Logic.DTO.Auth;
using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

public class AuthController : GlobalController
{
    private readonly IAuthService authService;

    public AuthController(IAuthService authService)
    {
        this.authService = authService;
    }

    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var (data, status) = await authService.RegisterAsync(request.Email, request.Password, request.FullName);
        return status switch
        {
            RegisterStatus.Success => Success(data),
            RegisterStatus.EmailAlreadyExists => Fail("כתובת המייל כבר רשומה במערכת"),
            _ => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (data, status) = await authService.LoginAsync(request.Email, request.Password);
        return status switch
        {
            LoginStatus.Success => Success(data),
            LoginStatus.InvalidCredentials => Fail("שם משתמש או סיסמא שגויים"),
            _ => Fail("שגיאה לא צפויה")
        };
    }

    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var (data, status) = await authService.GoogleLoginAsync(request.GoogleToken);
        return status switch
        {
            GoogleLoginStatus.Success => Success(data),
            GoogleLoginStatus.InvalidToken => Fail("Google token לא תקין"),
            _ => Fail("שגיאה לא צפויה")
        };
    }
}
