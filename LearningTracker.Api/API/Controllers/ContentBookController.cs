using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

[Authorize]
public class ContentBookController : GlobalController
{
    private readonly IContentCatalogService catalogService;

    public ContentBookController(IContentCatalogService catalogService)
    {
        this.catalogService = catalogService;
    }

    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        if (categoryId <= 0)
            return Fail("מזהה קטגוריה לא תקין");

        var result = await catalogService.GetBooksByCategoryAsync(categoryId);
        return Success(result);
    }

    public async Task<IActionResult> GetUnits(int bookId)
    {
        if (bookId <= 0)
            return Fail("מזהה ספר לא תקין");

        var result = await catalogService.GetUnitsByBookAsync(bookId);
        return Success(result);
    }
}
