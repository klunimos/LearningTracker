using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

[Authorize]
public class ContentCategoryController : GlobalController
{
    private readonly IContentCatalogService catalogService;

    public ContentCategoryController(IContentCatalogService catalogService)
    {
        this.catalogService = catalogService;
    }

    public async Task<IActionResult> GetAll()
    {
        var result = await catalogService.GetAllCategoriesAsync();
        return Success(result);
    }
}
