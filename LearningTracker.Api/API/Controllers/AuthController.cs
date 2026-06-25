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
    private readonly IConfiguration configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        this.authService = authService;
        this.configuration = configuration;
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

    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await authService.ForgotPasswordAsync(request.Email, ResolveClientBaseUrl());
        // Always succeed regardless of whether the email exists (no enumeration).
        return Success();
    }

    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var status = await authService.ResetPasswordAsync(request.Token, request.NewPassword);
        return status switch
        {
            ResetPasswordStatus.Success      => Success(),
            ResetPasswordStatus.InvalidToken => Fail("הקישור אינו תקין"),
            ResetPasswordStatus.Expired      => Fail("הקישור פג תוקף. נא לבקש איפוס סיסמה מחדש"),
            ResetPasswordStatus.Used         => Fail("הקישור כבר נוצל. נא לבקש איפוס סיסמה מחדש"),
            _                                => Fail("שגיאה לא צפויה")
        };
    }

    private string ResolveClientBaseUrl()
    {
        var origin = Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin))
            return origin;

        var configured = configuration["Frontend:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return configuration.GetSection("AllowedOrigins").Get<string[]>()?.FirstOrDefault()
            ?? "https://chelkenu.org";
    }
}
