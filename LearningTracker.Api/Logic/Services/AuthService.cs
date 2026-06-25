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
    Task<ForgotPasswordStatus> ForgotPasswordAsync(string email, string clientBaseUrl);
    Task<ResetPasswordStatus> ResetPasswordAsync(string token, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext db;
    private readonly IConfiguration configuration;
    private readonly IEmailSender emailSender;

    public AuthService(AppDbContext db, IConfiguration configuration, IEmailSender emailSender)
    {
        this.db = db;
        this.configuration = configuration;
        this.emailSender = emailSender;
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

    public async Task<ForgotPasswordStatus> ForgotPasswordAsync(string email, string clientBaseUrl)
    {
        var normalizedEmail = email.ToLower().Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Always report success so the response does not reveal whether an
        // account exists for the given address (prevents email enumeration).
        if (user == null)
            return ForgotPasswordStatus.Success;

        // Invalidate any outstanding reset tokens for this user.
        var outstanding = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();
        foreach (var old in outstanding)
            old.UsedAt = DateTime.UtcNow;

        var rawToken = GenerateResetToken();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var baseUrl = clientBaseUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/#/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendPasswordResetAsync(user.Email, resetLink);

        return ForgotPasswordStatus.Success;
    }

    public async Task<ResetPasswordStatus> ResetPasswordAsync(string token, string newPassword)
    {
        var tokenHash = HashToken(token);
        var stored = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored == null)
            return ResetPasswordStatus.InvalidToken;

        if (stored.UsedAt != null)
            return ResetPasswordStatus.Used;

        if (stored.ExpiresAt <= DateTime.UtcNow)
            return ResetPasswordStatus.Expired;

        stored.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        stored.User.UpdatedAt = DateTime.UtcNow;
        stored.UsedAt = DateTime.UtcNow;

        // Revoke all active refresh tokens so existing sessions can no longer
        // be silently extended after a password reset.
        var activeRefreshTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == stored.UserId && !rt.IsRevoked)
            .ToListAsync();
        foreach (var rt in activeRefreshTokens)
            rt.IsRevoked = true;

        await db.SaveChangesAsync();

        return ResetPasswordStatus.Success;
    }

    private static string GenerateResetToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
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
