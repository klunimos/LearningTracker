#nullable enable

using LearningTracker.Api.Data;
using LearningTracker.Api.Logic.DTO.Auth;
using Microsoft.EntityFrameworkCore;

namespace LearningTracker.Api.Logic.Services;

public interface IUserService
{
    Task<UserResponse?> GetMeAsync(int userId);
    Task<UserResponse?> UpdateProfileAsync(int userId, string fullName, string profilePicture);
}

public class UserService : IUserService
{
    private readonly AppDbContext db;

    public UserService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<UserResponse?> GetMeAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        return user == null ? null : MapToUserResponse(user);
    }

    public async Task<UserResponse?> UpdateProfileAsync(int userId, string fullName, string profilePicture)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return null;

        user.FullName = fullName;
        user.ProfilePicture = profilePicture;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return MapToUserResponse(user);
    }

    private static UserResponse MapToUserResponse(Data.Entities.User user)
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
