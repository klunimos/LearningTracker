namespace LearningTracker.Api.Logic.DTO.Auth;

public class AuthResponse
{
    public string Token { get; set; }
    public UserResponse User { get; set; }
}

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public bool IsAdmin { get; set; }
    public string ProfilePicture { get; set; }
}
