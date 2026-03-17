using System.Security.Claims;
using LearningTracker.Api.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public abstract class GlobalController : ControllerBase
{
    protected int UserId
    {
        get
        {
            var sub = User.FindFirstValue("sub")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(sub, out var id) ? id : 0;
        }
    }

    protected IActionResult Success<T>(T value)
    {
        return base.Ok(new ResultData<T> { Success = true, Value = value });
    }

    protected IActionResult Success()
    {
        return base.Ok(new ResultData<object> { Success = true });
    }

    protected IActionResult Fail(string message)
    {
        return base.Ok(new ResultData<object> { Success = false, Message = message });
    }
}

