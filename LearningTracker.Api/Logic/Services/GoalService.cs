using LearningTracker.Api.Data;
using LearningTracker.Api.Data.Entities;
using LearningTracker.Api.Logic.DTO.Goal;
using Microsoft.EntityFrameworkCore;

namespace LearningTracker.Api.Logic.Services;

public interface IGoalService
{
    Task<List<GoalSummaryResponse>> GetMyGoalsAsync(int userId, bool includeInactive = false, int take = 50);
    Task<(GoalSummaryResponse response, bool found)> GetByIdAsync(int userId, int goalId);
    Task<(GoalSummaryResponse response, CreateGoalStatus status)> CreateGoalAsync(int userId, CreateGoalRequest request);
    Task<(GoalSummaryResponse response, ReportProgressStatus status)> ReportProgressAsync(int userId, ReportProgressRequest request);
    Task<(GoalSummaryResponse response, SetActiveStatus status)> SetActiveAsync(int userId, SetActiveRequest request);
    Task<(PaceCalcResponse response, bool found)> CalculatePaceAsync(int bookId, int startUnitId, DateOnly targetDate);
    Task<(TargetDateCalcResponse response, bool found)> CalculateTargetDateAsync(int bookId, int startUnitId, decimal dailyPace);
}

public class GoalService : IGoalService
{
    private readonly AppDbContext db;

    public GoalService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<GoalSummaryResponse>> GetMyGoalsAsync(int userId, bool includeInactive = false, int take = 50)
    {
        var query = db.Goals
            .Include(g => g.GoalBooks)
            .Include(g => g.Category)
            .Include(g => g.StartUnit)
            .Where(g => g.UserId == userId);

        if (!includeInactive)
        {
            query = query.Where(g => g.IsActive);
        }

        var goals = await query
            .OrderByDescending(g => g.CreatedAt)
            .Take(take)
            .ToListAsync();

        var result = new List<GoalSummaryResponse>();
        foreach (var goal in goals)
        {
            result.Add(await BuildGoalSummaryAsync(goal));
        }
        return result;
    }

    public async Task<(GoalSummaryResponse response, bool found)> GetByIdAsync(int userId, int goalId)
    {
        var goal = await db.Goals
            .Include(g => g.GoalBooks)
            .Include(g => g.Category)
            .Include(g => g.StartUnit)
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null) return (null, false);

        return (await BuildGoalSummaryAsync(goal), true);
    }

    public async Task<(GoalSummaryResponse response, SetActiveStatus status)> SetActiveAsync(int userId, SetActiveRequest request)
    {
        var goal = await db.Goals
            .Include(g => g.GoalBooks)
            .Include(g => g.Category)
            .Include(g => g.StartUnit)
            .FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == userId);
        if (goal == null)
            return (null, SetActiveStatus.GoalNotFound);

        goal.IsActive  = request.IsActive;
        goal.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var summary = await BuildGoalSummaryAsync(goal);
        return (summary, SetActiveStatus.Success);
    }

    public async Task<(GoalSummaryResponse response, CreateGoalStatus status)> CreateGoalAsync(int userId, CreateGoalRequest request)
    {
        bool isCategoryGoal = request.CategoryId.HasValue && request.BookIds.Count == 0;

        if (!isCategoryGoal && request.BookIds.Count == 0)
            return (null, CreateGoalStatus.NoBooksSpecified);

        if (request.CategoryId.HasValue)
        {
            bool categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value);
            if (!categoryExists)
                return (null, CreateGoalStatus.CategoryNotFound);
        }

        if (request.BookIds.Count > 0)
        {
            int foundCount = await db.Books.CountAsync(b => request.BookIds.Contains(b.Id));
            if (foundCount != request.BookIds.Count)
                return (null, CreateGoalStatus.BookNotFound);
        }

        if (request.StartUnitId.HasValue)
        {
            bool unitExists = await db.BookUnits.AnyAsync(u => u.Id == request.StartUnitId.Value);
            if (!unitExists)
                return (null, CreateGoalStatus.StartUnitNotFound);
        }

        var goal = new Goal
        {
            UserId = userId,
            CategoryId = isCategoryGoal ? request.CategoryId : null,
            Title = request.Title,
            StartUnitId = request.StartUnitId,
            TargetDate = request.TargetDate,
            DailyPace = request.DailyPace,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (!isCategoryGoal)
        {
            goal.GoalBooks = request.BookIds
                .Select(bookId => new GoalBook { BookId = bookId })
                .ToList();
        }

        db.Goals.Add(goal);
        await db.SaveChangesAsync();

        var summary = await BuildGoalSummaryAsync(goal);
        return (summary, CreateGoalStatus.Success);
    }

    public async Task<(GoalSummaryResponse response, ReportProgressStatus status)> ReportProgressAsync(int userId, ReportProgressRequest request)
    {
        if (request.UnitIds == null || request.UnitIds.Count == 0)
            return (null, ReportProgressStatus.NoUnitsSpecified);

        var goal = await db.Goals
            .Include(g => g.GoalBooks)
            .Include(g => g.Category)
            .Include(g => g.StartUnit)
            .FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == userId);
        if (goal == null)
            return (null, ReportProgressStatus.GoalNotFound);

        bool bookInGoal = goal.CategoryId.HasValue
            ? await db.Books.AnyAsync(b => b.Id == request.BookId && b.CategoryId == goal.CategoryId.Value)
            : await db.GoalBooks.AnyAsync(gb => gb.GoalId == goal.Id && gb.BookId == request.BookId);

        if (!bookInGoal)
            return (null, ReportProgressStatus.BookNotInGoal);

        var allUnits = await db.BookUnits
            .Where(u => u.BookId == request.BookId)
            .OrderBy(u => u.SortOrder)
            .ToListAsync();

        var unitById = allUnits.ToDictionary(u => u.Id);

        foreach (var uid in request.UnitIds)
        {
            if (!unitById.ContainsKey(uid))
                return (null, ReportProgressStatus.UnitNotInBook);
        }

        var existingEntries = await db.ProgressEntries
            .Where(pe => pe.GoalId == goal.Id && pe.BookId == request.BookId)
            .ToListAsync();

        var alreadyReportedOrders = new HashSet<int>();
        foreach (var entry in existingEntries)
        {
            if (!unitById.TryGetValue(entry.FromUnitId, out var ef)) continue;
            if (!unitById.TryGetValue(entry.ToUnitId, out var et)) continue;
            foreach (var u in allUnits.Where(u => u.SortOrder >= ef.SortOrder && u.SortOrder <= et.SortOrder))
            {
                alreadyReportedOrders.Add(u.SortOrder);
            }
        }

        var requestedOrders = request.UnitIds.Select(uid => unitById[uid].SortOrder).ToHashSet();
        if (requestedOrders.Any(o => alreadyReportedOrders.Contains(o)))
            return (null, ReportProgressStatus.UnitsAlreadyReported);

        var ranges = GroupIntoContiguousRanges(allUnits, request.UnitIds);

        var now = DateTime.UtcNow;
        foreach (var (from, to) in ranges)
        {
            db.ProgressEntries.Add(new ProgressEntry
            {
                GoalId     = goal.Id,
                UserId     = userId,
                BookId     = request.BookId,
                FromUnitId = from.Id,
                ToUnitId   = to.Id,
                Note       = request.Note,
                ReportedAt = now
            });
        }

        goal.UpdatedAt = now;
        await db.SaveChangesAsync();

        var summary = await BuildGoalSummaryAsync(goal);
        return (summary, ReportProgressStatus.Success);
    }

    public async Task<(PaceCalcResponse response, bool found)> CalculatePaceAsync(int bookId, int startUnitId, DateOnly targetDate)
    {
        var startUnit = await db.BookUnits.FirstOrDefaultAsync(u => u.Id == startUnitId && u.BookId == bookId);
        if (startUnit == null)
            return (null, false);

        int totalRemaining = await db.BookUnits
            .CountAsync(u => u.BookId == bookId && u.SortOrder >= startUnit.SortOrder);

        int daysRemaining = targetDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        if (daysRemaining <= 0)
        {
            daysRemaining = 1;
        }

        decimal pace = Math.Ceiling((decimal)totalRemaining / daysRemaining * 10) / 10;

        return (new PaceCalcResponse
        {
            RequiredDailyPace = pace,
            TotalUnitsRemaining = totalRemaining,
            DaysRemaining = daysRemaining
        }, true);
    }

    public async Task<(TargetDateCalcResponse response, bool found)> CalculateTargetDateAsync(int bookId, int startUnitId, decimal dailyPace)
    {
        var startUnit = await db.BookUnits.FirstOrDefaultAsync(u => u.Id == startUnitId && u.BookId == bookId);
        if (startUnit == null)
            return (null, false);

        if (dailyPace <= 0)
        {
            dailyPace = 1;
        }

        int totalRemaining = await db.BookUnits
            .CountAsync(u => u.BookId == bookId && u.SortOrder >= startUnit.SortOrder);

        int daysNeeded = (int)Math.Ceiling(totalRemaining / (double)dailyPace);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysNeeded);

        return (new TargetDateCalcResponse
        {
            TargetDate = targetDate,
            TotalUnitsRemaining = totalRemaining,
            DaysNeeded = daysNeeded
        }, true);
    }

    private async Task<GoalSummaryResponse> BuildGoalSummaryAsync(Goal goal)
    {
        List<int> bookIds = await GetGoalBookIdsAsync(goal);

        var startUnit = goal.StartUnitId.HasValue
            ? await db.BookUnits.FindAsync(goal.StartUnitId.Value)
            : null;

        var allBooks = await db.Books
            .Where(b => bookIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        var allUnits = await db.BookUnits
            .Where(u => bookIds.Contains(u.BookId))
            .OrderBy(u => u.SortOrder)
            .ToListAsync();

        var unitsByBook = allUnits.GroupBy(u => u.BookId).ToDictionary(g => g.Key, g => g.ToList());

        var allEntries = await db.ProgressEntries
            .Where(pe => pe.GoalId == goal.Id && bookIds.Contains(pe.BookId))
            .ToListAsync();

        var entriesByBook = allEntries.GroupBy(e => e.BookId).ToDictionary(g => g.Key, g => g.ToList());

        int totalUnits = 0;
        int completedUnits = 0;
        var bookProgresses = new List<BookProgressResponse>();

        foreach (var bookId in bookIds)
        {
            var book = allBooks[bookId];
            var bookUnits = unitsByBook.GetValueOrDefault(bookId) ?? new List<BookUnit>();
            var entries = entriesByBook.GetValueOrDefault(bookId) ?? new List<ProgressEntry>();

            var progress = BuildBookProgress(goal, book, bookUnits, entries, startUnit);
            bookProgresses.Add(progress);

            totalUnits += progress.TotalUnits;
            completedUnits += (int)Math.Round(progress.ProgressPercent / 100.0 * progress.TotalUnits);
        }

        double progressPercent = totalUnits > 0 ? Math.Round((double)completedUnits / totalUnits * 100, 1) : 0;

        bool isOnTrack = true;
        if (goal.DailyPace.HasValue && goal.DailyPace.Value > 0)
        {
            double daysSinceStart = (DateTime.UtcNow - goal.CreatedAt).TotalDays;
            double expectedUnits = (double)goal.DailyPace.Value * daysSinceStart;
            isOnTrack = completedUnits >= expectedUnits;
        }

        string scopeName = goal.CategoryId.HasValue
            ? goal.Category?.Name ?? (await db.Categories.FirstAsync(c => c.Id == goal.CategoryId.Value)).Name
            : string.Join(", ", bookProgresses.Select(b => b.BookName));

        return new GoalSummaryResponse
        {
            Id = goal.Id,
            Title = goal.Title,
            IsCategoryGoal = goal.CategoryId.HasValue,
            ScopeName = scopeName,
            TotalUnits = totalUnits,
            CompletedUnits = completedUnits,
            ProgressPercent = progressPercent,
            TargetDate = goal.TargetDate,
            DailyPace = goal.DailyPace,
            IsOnTrack = isOnTrack,
            IsCompleted = goal.IsCompleted,
            IsActive = goal.IsActive,
            Books = bookProgresses
        };
    }

    private async Task<List<int>> GetGoalBookIdsAsync(Goal goal)
    {
        if (goal.CategoryId.HasValue)
        {
            return await db.Books
                .Where(b => b.CategoryId == goal.CategoryId.Value)
                .Select(b => b.Id)
                .ToListAsync();
        }

        return goal.GoalBooks.Select(gb => gb.BookId).ToList();
    }

    private BookProgressResponse BuildBookProgress(Goal goal, Book book, List<BookUnit> bookUnits, List<ProgressEntry> entries, BookUnit startUnit)
    {
        var unitById = bookUnits.ToDictionary(u => u.Id);

        int startSortOrder = startUnit != null && startUnit.BookId == book.Id
            ? startUnit.SortOrder
            : (bookUnits.Count > 0 ? bookUnits[0].SortOrder : 1);

        int bookTotal = bookUnits.Count(u => u.SortOrder >= startSortOrder);

        var coveredOrders = ExpandCoveredSortOrders(bookUnits, entries, unitById);

        int bookCompleted = bookUnits.Count(u => u.SortOrder >= startSortOrder && coveredOrders.Contains(u.SortOrder));

        var reportedUnitIds = bookUnits
            .Where(u => coveredOrders.Contains(u.SortOrder))
            .Select(u => u.Id)
            .ToList();

        string currentUnitName = null;
        int currentUnitId = 0;
        var latestCovered = bookUnits
            .Where(u => u.SortOrder >= startSortOrder && coveredOrders.Contains(u.SortOrder))
            .OrderByDescending(u => u.SortOrder)
            .FirstOrDefault();
        if (latestCovered != null)
        {
            currentUnitName = latestCovered.DisplayName;
            currentUnitId   = latestCovered.Id;
        }

        double bookPercent = bookTotal > 0 ? Math.Round((double)bookCompleted / bookTotal * 100, 1) : 0;

        string expectedUnitName = null;
        int? expectedUnitId = null;
        if (goal.TargetDate.HasValue && bookTotal > 0)
        {
            var expectedUnit = CalculateExpectedUnit(goal, bookUnits, startSortOrder, bookTotal);
            if (expectedUnit != null)
            {
                expectedUnitName = expectedUnit.DisplayName;
                expectedUnitId   = expectedUnit.Id;
            }
        }

        return new BookProgressResponse
        {
            BookId          = book.Id,
            BookName        = book.Name,
            CurrentUnitName = currentUnitName,
            CurrentUnitId   = currentUnitId,
            ExpectedUnitName = expectedUnitName,
            ExpectedUnitId  = expectedUnitId,
            TotalUnits      = bookTotal,
            ProgressPercent = bookPercent,
            ReportedUnitIds = reportedUnitIds
        };
    }

    private static HashSet<int> ExpandCoveredSortOrders(List<BookUnit> bookUnits, List<ProgressEntry> entries, Dictionary<int, BookUnit> unitById)
    {
        var coveredOrders = new HashSet<int>();
        foreach (var entry in entries)
        {
            if (!unitById.TryGetValue(entry.FromUnitId, out var ef)) continue;
            if (!unitById.TryGetValue(entry.ToUnitId, out var et)) continue;
            foreach (var u in bookUnits.Where(u => u.SortOrder >= ef.SortOrder && u.SortOrder <= et.SortOrder))
            {
                coveredOrders.Add(u.SortOrder);
            }
        }
        return coveredOrders;
    }

    private static BookUnit CalculateExpectedUnit(Goal goal, List<BookUnit> bookUnits, int startSortOrder, int bookTotal)
    {
        var goalStartDate  = goal.CreatedAt.Date;
        var targetDateTime = goal.TargetDate.Value.ToDateTime(TimeOnly.MinValue);
        double totalDays   = Math.Max(1, (targetDateTime - goalStartDate).TotalDays);
        double daysElapsed = (DateTime.UtcNow - goal.CreatedAt).TotalDays;
        double ratio       = Math.Min(1, daysElapsed / totalDays);
        int expectedIndex  = Math.Max(1, (int)(ratio * bookTotal));

        return bookUnits
            .Where(u => u.SortOrder >= startSortOrder)
            .OrderBy(u => u.SortOrder)
            .Skip(expectedIndex - 1)
            .FirstOrDefault();
    }

    private static List<(BookUnit from, BookUnit to)> GroupIntoContiguousRanges(List<BookUnit> allUnits, List<int> selectedIds)
    {
        var selectedSet = new HashSet<int>(selectedIds);
        var ranges = new List<(BookUnit from, BookUnit to)>();
        BookUnit rangeStart = null;
        BookUnit rangeEnd   = null;

        foreach (var unit in allUnits)
        {
            if (selectedSet.Contains(unit.Id))
            {
                rangeStart ??= unit;
                rangeEnd    = unit;
            }
            else if (rangeStart != null)
            {
                ranges.Add((rangeStart, rangeEnd));
                rangeStart = null;
                rangeEnd   = null;
            }
        }
        if (rangeStart != null)
        {
            ranges.Add((rangeStart, rangeEnd));
        }

        return ranges;
    }
}
