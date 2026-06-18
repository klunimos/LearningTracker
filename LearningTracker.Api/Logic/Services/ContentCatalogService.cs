using LearningTracker.Api.Data;
using LearningTracker.Api.Logic.DTO.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LearningTracker.Api.Logic.Services;

public interface IContentCatalogService
{
    Task<List<CategoryResponse>> GetAllCategoriesAsync();
    Task<List<BookSummaryResponse>> GetBooksByCategoryAsync(int categoryId);
    Task<List<UnitGroupResponse>> GetUnitsByBookAsync(int bookId);
}

public class ContentCatalogService : IContentCatalogService
{
    private readonly AppDbContext db;

    public ContentCatalogService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<CategoryResponse>> GetAllCategoriesAsync()
    {
        return await db.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                L1Name = c.L1Name,
                L2Name = c.L2Name,
                UnitName = c.UnitName
            })
            .ToListAsync();
    }

    public async Task<List<BookSummaryResponse>> GetBooksByCategoryAsync(int categoryId)
    {
        return await db.Books
            .Where(b => b.CategoryId == categoryId)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .Select(b => new BookSummaryResponse
            {
                Id = b.Id,
                CategoryId = b.CategoryId,
                Name = b.Name,
                SeriesName = b.SeriesName,
                TotalUnits = b.Units.Count
            })
            .ToListAsync();
    }

    public async Task<List<UnitGroupResponse>> GetUnitsByBookAsync(int bookId)
    {
        var units = await db.BookUnits
            .Where(u => u.BookId == bookId)
            .OrderBy(u => u.SortOrder)
            .ToListAsync();

        return units
            .GroupBy(u => new { u.L1Label, u.L1Order })
            .OrderBy(g => g.Key.L1Order)
            .Select(g => new UnitGroupResponse
            {
                L1Label = g.Key.L1Label,
                L1Order = g.Key.L1Order,
                Units = g.Select(u => new UnitResponse
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    SortOrder = u.SortOrder
                }).ToList()
            })
            .ToList();
    }
}
