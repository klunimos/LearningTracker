using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using LearningTracker.Api.Data;
using LearningTracker.Api.Data.Entities;
using LearningTracker.Api.Logic.DTO.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LearningTracker.Api.Logic.Services;

public interface IAuthService
{
    Task<(AuthResponse, RegisterStatus)> RegisterAsync(string email, string password, string fullName);
    Task<(AuthResponse, LoginStatus)> LoginAsync(string email, string password);
    Task<(AuthResponse, GoogleLoginStatus)> GoogleLoginAsync(string googleToken);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext db;
    private readonly IConfiguration configuration;

    public AuthService(AppDbContext db, IConfiguration configuration)
    {
        this.db = db;
        this.configuration = configuration;
    }

    public async Task<(AuthResponse, RegisterStatus)> RegisterAsync(string email, string password, string fullName)
    {
        var normalizedEmail = email.ToLower().Trim();
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail))
            return (null, RegisterStatus.EmailAlreadyExists);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (BuildAuthResponse(user), RegisterStatus.Success);
    }

    public async Task<(AuthResponse, LoginStatus)> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.ToLower().Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (null, LoginStatus.InvalidCredentials);

        return (BuildAuthResponse(user), LoginStatus.Success);
    }

    public async Task<(AuthResponse, GoogleLoginStatus)> GoogleLoginAsync(string googleToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(googleToken);
        }
        catch
        {
            return (null, GoogleLoginStatus.InvalidToken);
        }

        var normalizedEmail = payload.Email.ToLower().Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject || u.Email == normalizedEmail);

        if (user == null)
        {
            user = new User
            {
                Email = normalizedEmail,
                FullName = payload.Name,
                GoogleId = payload.Subject,
                ProfilePicture = payload.Picture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
        }
        else if (user.GoogleId == null)
        {
            user.GoogleId = payload.Subject;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        return (BuildAuthResponse(user), GoogleLoginStatus.Success);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        return new AuthResponse
        {
            Token = GenerateToken(user),
            User = MapToUserResponse(user)
        };
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.FullName),
            new Claim("isAdmin", user.IsAdmin.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserResponse MapToUserResponse(User user)
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
