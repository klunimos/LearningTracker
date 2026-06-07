using LearningTracker.Api.Logic.DTO.Auth;
using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearningTracker.Api.API.Controllers;

[AllowAnonymous]
[EnableRateLimiting("auth")]
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

    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var (data, status) = await authService.RefreshAsync(request.RefreshToken);
        return status switch
        {
            RefreshStatus.Success      => Success(data),
            RefreshStatus.InvalidToken => Fail("טוקן לא תקין"),
            RefreshStatus.Expired      => Fail("טוקן פג תוקף"),
            RefreshStatus.Revoked      => Fail("טוקן בוטל"),
            _                          => Fail("שגיאה לא צפויה")
        };
    }
}
