using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    Task<(AuthResponse, RefreshStatus)> RefreshAsync(string refreshToken);
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

        return (await BuildAuthResponseAsync(user), RegisterStatus.Success);
    }

    public async Task<(AuthResponse, LoginStatus)> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.ToLower().Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (null, LoginStatus.InvalidCredentials);

        return (await BuildAuthResponseAsync(user), LoginStatus.Success);
    }

    public async Task<(AuthResponse, GoogleLoginStatus)> GoogleLoginAsync(string googleToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            var clientId = configuration["Google:ClientId"];
            if (!string.IsNullOrWhiteSpace(clientId) && !clientId.StartsWith("YOUR_") && !clientId.StartsWith("REPLACE_"))
                settings.Audience = new[] { clientId };

            payload = await GoogleJsonWebSignature.ValidateAsync(googleToken, settings);
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

        return (await BuildAuthResponseAsync(user), GoogleLoginStatus.Success);
    }

    public async Task<(AuthResponse, RefreshStatus)> RefreshAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored == null)
            return (null, RefreshStatus.InvalidToken);

        if (stored.IsRevoked)
            return (null, RefreshStatus.Revoked);

        if (stored.ExpiresAt <= DateTime.UtcNow)
            return (null, RefreshStatus.Expired);

        stored.IsRevoked = true;
        await db.SaveChangesAsync();

        return (await BuildAuthResponseAsync(stored.User), RefreshStatus.Success);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var refreshTokenValue = GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        });
        await db.SaveChangesAsync();

        return new AuthResponse
        {
            Token = GenerateToken(user),
            RefreshToken = refreshTokenValue,
            User = UserResponse.FromEntity(user)
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
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
