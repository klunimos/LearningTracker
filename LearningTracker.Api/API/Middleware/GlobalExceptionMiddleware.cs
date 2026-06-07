using System.Text.Json;
using LearningTracker.Api.API.Models;

namespace LearningTracker.Api.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<GlobalExceptionMiddleware> logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";

            var result = new ResultData<object>
            {
                Success = false,
                Message = "שגיאה לא צפויה. נסה שוב מאוחר יותר."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(result, JsonOptions));
        }
    }
}
