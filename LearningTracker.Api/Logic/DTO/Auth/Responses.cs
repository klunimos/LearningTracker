namespace LearningTracker.Api.Logic.DTO.Auth;

public class AuthResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public UserResponse User { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public bool IsAdmin { get; set; }
    public string ProfilePicture { get; set; }

    public static UserResponse FromEntity(Data.Entities.User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            IsAdmin = user.IsAdmin,
            ProfilePicture = user.ProfilePicture
        };
    }
}
