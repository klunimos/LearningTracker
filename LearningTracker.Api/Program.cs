using System.Text;
using System.Threading.RateLimiting;
using ElmahCore.Mvc;
using LearningTracker.Api.API.Middleware;
using LearningTracker.Api.Data;
using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LearningTracker.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);
        var app = builder.Build();
        Configure(app);
        app.Run();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        var jwtKey = builder.Configuration["Jwt:Key"]!;
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("auth", limiter =>
            {
                limiter.PermitLimit = 10;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IContentCatalogService, ContentCatalogService>();
        builder.Services.AddScoped<IGoalService, GoalService>();
        builder.Services.AddScoped<IGroupService, GroupService>();
        builder.Services.AddScoped<IGroupGoalService, GroupGoalService>();

        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        if (allowedOrigins?.Length > 0)
        {
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
                });
            });
        }

        builder.Services.AddElmah(options =>
        {
            options.OnPermissionCheck = ctx => ctx.User.Identity?.IsAuthenticated == true;
        });
    }

    private static void Configure(WebApplication app)
    {
        // Running behind a reverse proxy (Caddy) that terminates TLS.
        // Honor X-Forwarded-* so the app sees the original https scheme / client IP.
        var forwardedOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        forwardedOptions.KnownNetworks.Clear();
        forwardedOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedOptions);

        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        if (app.Configuration.GetSection("AllowedOrigins").Get<string[]>()?.Length > 0)
        {
            app.UseCors();
        }

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseElmah();
        app.MapControllers();
    }
}
