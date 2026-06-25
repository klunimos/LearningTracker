using System.ComponentModel.DataAnnotations;

namespace LearningTracker.Api.Logic.DTO.Auth;

public class RegisterRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Email { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string FullName { get; set; }
}

public class LoginRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Email { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; }
}

public class GoogleLoginRequest
{
    [Required(AllowEmptyStrings = false)]
    public string GoogleToken { get; set; }
}

public class RefreshTokenRequest
{
    [Required(AllowEmptyStrings = false)]
    public string RefreshToken { get; set; }
}

public class ForgotPasswordRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Email { get; set; }
}

public class ResetPasswordRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string NewPassword { get; set; }
}
