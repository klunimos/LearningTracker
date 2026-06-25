namespace LearningTracker.Api.Data.Entities;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User User { get; set; }
}
