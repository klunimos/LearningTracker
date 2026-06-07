using LearningTracker.Api.Data;
using LearningTracker.Api.Data.Entities;
using LearningTracker.Api.Logic.DTO.Group;
using LearningTracker.Api.Logic.DTO.GroupGoal;
using Microsoft.EntityFrameworkCore;

namespace LearningTracker.Api.Logic.Services;

public interface IGroupGoalService
{
    Task<List<GroupGoalSummaryResponse>> GetByGroupAsync(int userId, int groupId);
    Task<List<GroupGoalHomeItemResponse>> GetMyParticipatingGoalsAsync(int userId);
    Task<(GroupGoalDetailResponse response, CreateGroupGoalStatus status)> CreateGroupGoalAsync(int userId, CreateGroupGoalRequest request);
    Task<(GroupGoalDetailResponse response, JoinGroupGoalStatus status)> JoinGroupGoalAsync(int userId, JoinGroupGoalRequest request);
    Task<(GroupGoalDetailResponse response, ReportGroupProgressStatus status)> ReportProgressAsync(int userId, ReportGroupProgressRequest request);
    Task<(List<MemberProgressResponse> response, bool found)> GetMembersProgressAsync(int userId, int groupGoalId);
}

public class GroupGoalService : IGroupGoalService
{
    private readonly AppDbContext db;

    public GroupGoalService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<GroupGoalSummaryResponse>> GetByGroupAsync(int userId, int groupId)
    {
        var group = await db.Groups
            .Include(g => g.GroupGoals).ThenInclude(gg => gg.GroupGoalMembers)
            .Include(g => g.GroupGoals).ThenInclude(gg => gg.GroupGoalBooks)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return new List<GroupGoalSummaryResponse>();

        var result = new List<GroupGoalSummaryResponse>();
        foreach (var goal in group.GroupGoals)
        {
            bool isParticipating = goal.GroupGoalMembers.Any(m => m.UserId == userId);
            result.Add(await BuildGoalSummaryAsync(goal, isParticipating));
        }
        return result;
    }

    public async Task<List<GroupGoalHomeItemResponse>> GetMyParticipatingGoalsAsync(int userId)
    {
        var goals = await db.GroupGoals
            .Include(g => g.Group)
            .Include(g => g.Category)
            .Include(g => g.GroupGoalBooks).ThenInclude(ggb => ggb.Book)
            .Where(g => g.GroupGoalMembers.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        var goalIds = goals.Select(g => g.Id).ToList();
        var allBookIds = goals
            .SelectMany(g => g.GroupGoalBooks.Select(ggb => ggb.BookId))
            .Distinct()
            .ToList();

        var unitCountsByBook = await db.BookUnits
            .Where(u => allBookIds.Contains(u.BookId))
            .GroupBy(u => u.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId, x => x.Count);

        var latestEntries = await db.GroupProgressEntries
            .Where(pe => goalIds.Contains(pe.GroupGoalId) && pe.UserId == userId)
            .GroupBy(pe => new { pe.GroupGoalId, pe.BookId })
            .Select(g => g.OrderByDescending(pe => pe.ReportedAt).First())
            .ToListAsync();

        var latestUnitIds = latestEntries.Select(e => e.UnitId).Distinct().ToList();
        var latestUnits = await db.BookUnits
            .Where(u => latestUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var allUnitsByBook = new Dictionary<int, List<BookUnit>>();
        foreach (var goal in goals.Where(g => g.TargetDate.HasValue))
        {
            var bIds = goal.GroupGoalBooks.Select(ggb => ggb.BookId).ToList();
            foreach (var bookId in bIds)
            {
                if (!allUnitsByBook.ContainsKey(bookId))
                {
                    allUnitsByBook[bookId] = await db.BookUnits
                        .Where(u => u.BookId == bookId)
                        .OrderBy(u => u.SortOrder)
                        .ToListAsync();
                }
            }
        }

        var result = new List<GroupGoalHomeItemResponse>();
        foreach (var goal in goals)
        {
            result.Add(BuildGroupGoalHomeItem(goal, userId, unitCountsByBook, latestEntries, latestUnits, allUnitsByBook));
        }
        return result;
    }

    public async Task<(GroupGoalDetailResponse response, CreateGroupGoalStatus status)> CreateGroupGoalAsync(int userId, CreateGroupGoalRequest request)
    {
        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId);
        if (group == null)
            return (null, CreateGroupGoalStatus.GroupNotFound);

        bool isAdmin = group.Members.Any(m => m.UserId == userId && m.Role == GroupRoles.Admin);
        if (!isAdmin)
            return (null, CreateGroupGoalStatus.NotGroupAdmin);

        bool isCategoryGoal = request.CategoryId.HasValue && request.BookIds.Count == 0;

        if (!isCategoryGoal && request.BookIds.Count == 0)
            return (null, CreateGroupGoalStatus.NoBooksSpecified);

        if (request.CategoryId.HasValue)
        {
            bool categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value);
            if (!categoryExists)
                return (null, CreateGroupGoalStatus.CategoryNotFound);
        }

        if (request.BookIds.Count > 0)
        {
            int foundCount = await db.Books.CountAsync(b => request.BookIds.Contains(b.Id));
            if (foundCount != request.BookIds.Count)
                return (null, CreateGroupGoalStatus.BookNotFound);
        }

        var goal = new GroupGoal
        {
            GroupId = request.GroupId,
            CategoryId = isCategoryGoal ? request.CategoryId : null,
            Title = request.Title,
            TargetDate = request.TargetDate,
            CollectiveTargetUnitId = request.CollectiveTargetUnitId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            GroupGoalMembers = new List<GroupGoalMember>()
        };

        if (!isCategoryGoal)
        {
            goal.GroupGoalBooks = request.BookIds
                .Select(bookId => new GroupGoalBook { BookId = bookId })
                .ToList();
        }

        db.GroupGoals.Add(goal);
        await db.SaveChangesAsync();

        var response = await BuildGoalDetailAsync(goal, false);
        return (response, CreateGroupGoalStatus.Success);
    }

    public async Task<(GroupGoalDetailResponse response, JoinGroupGoalStatus status)> JoinGroupGoalAsync(int userId, JoinGroupGoalRequest request)
    {
        var goal = await db.GroupGoals
            .Include(g => g.Group).ThenInclude(g => g.Members)
            .Include(g => g.GroupGoalMembers)
            .Include(g => g.GroupGoalBooks)
            .FirstOrDefaultAsync(g => g.Id == request.GroupGoalId);
        if (goal == null)
            return (null, JoinGroupGoalStatus.GoalNotFound);

        bool isGroupMember = goal.Group.Members.Any(m => m.UserId == userId);
        if (!isGroupMember)
            return (null, JoinGroupGoalStatus.NotGroupMember);

        bool alreadyParticipating = goal.GroupGoalMembers.Any(m => m.UserId == userId);
        if (alreadyParticipating)
            return (null, JoinGroupGoalStatus.AlreadyParticipating);

        var newMember = new GroupGoalMember
        {
            GroupGoalId = goal.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        db.GroupGoalMembers.Add(newMember);
        await db.SaveChangesAsync();

        await db.Entry(newMember).Reference(m => m.User).LoadAsync();

        var response = await BuildGoalDetailAsync(goal, true);
        return (response, JoinGroupGoalStatus.Success);
    }

    public async Task<(GroupGoalDetailResponse response, ReportGroupProgressStatus status)> ReportProgressAsync(int userId, ReportGroupProgressRequest request)
    {
        var goal = await db.GroupGoals
            .Include(g => g.GroupGoalMembers).ThenInclude(m => m.User)
            .Include(g => g.GroupGoalBooks)
            .FirstOrDefaultAsync(g => g.Id == request.GroupGoalId);
        if (goal == null)
            return (null, ReportGroupProgressStatus.GoalNotFound);

        bool isParticipating = goal.GroupGoalMembers.Any(m => m.UserId == userId);
        if (!isParticipating)
            return (null, ReportGroupProgressStatus.NotParticipating);

        bool bookInGoal = goal.CategoryId.HasValue
            ? await db.Books.AnyAsync(b => b.Id == request.BookId && b.CategoryId == goal.CategoryId.Value)
            : await db.GroupGoalBooks.AnyAsync(ggb => ggb.GroupGoalId == goal.Id && ggb.BookId == request.BookId);

        if (!bookInGoal)
            return (null, ReportGroupProgressStatus.BookNotInGoal);

        bool unitInBook = await db.BookUnits.AnyAsync(u => u.Id == request.UnitId && u.BookId == request.BookId);
        if (!unitInBook)
            return (null, ReportGroupProgressStatus.UnitNotInBook);

        db.GroupProgressEntries.Add(new GroupProgressEntry
        {
            GroupGoalId = goal.Id,
            UserId = userId,
            BookId = request.BookId,
            UnitId = request.UnitId,
            IsCollectiveTarget = request.IsCollectiveTarget,
            ReportedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var response = await BuildGoalDetailAsync(goal, true);
        return (response, ReportGroupProgressStatus.Success);
    }

    public async Task<(List<MemberProgressResponse> response, bool found)> GetMembersProgressAsync(int userId, int groupGoalId)
    {
        var goal = await db.GroupGoals
            .Include(g => g.Group).ThenInclude(g => g.Members)
            .Include(g => g.GroupGoalMembers).ThenInclude(m => m.User)
            .Include(g => g.GroupGoalBooks)
            .FirstOrDefaultAsync(g => g.Id == groupGoalId);
        if (goal == null)
            return (null, false);

        bool isGroupMember = goal.Group.Members.Any(m => m.UserId == userId);
        if (!isGroupMember)
            return (null, false);

        var result = await BuildMembersProgressAsync(goal);
        return (result, true);
    }

    private async Task<GroupGoalSummaryResponse> BuildGoalSummaryAsync(GroupGoal goal, bool isParticipating)
    {
        string scopeName = await GetScopeNameAsync(goal);

        return new GroupGoalSummaryResponse
        {
            Id = goal.Id,
            GroupId = goal.GroupId,
            Title = goal.Title,
            IsCategoryGoal = goal.CategoryId.HasValue,
            ScopeName = scopeName,
            TargetDate = goal.TargetDate,
            MemberCount = goal.GroupGoalMembers.Count,
            IsParticipating = isParticipating,
            CreatedAt = goal.CreatedAt
        };
    }

    private async Task<GroupGoalDetailResponse> BuildGoalDetailAsync(GroupGoal goal, bool isParticipating)
    {
        string scopeName = await GetScopeNameAsync(goal);
        var membersProgress = await BuildMembersProgressAsync(goal);

        return new GroupGoalDetailResponse
        {
            Id = goal.Id,
            GroupId = goal.GroupId,
            Title = goal.Title,
            IsCategoryGoal = goal.CategoryId.HasValue,
            ScopeName = scopeName,
            TargetDate = goal.TargetDate,
            IsParticipating = isParticipating,
            MembersProgress = membersProgress,
            CreatedAt = goal.CreatedAt
        };
    }

    private async Task<List<MemberProgressResponse>> BuildMembersProgressAsync(GroupGoal goal)
    {
        List<int> bookIds = await GetGoalBookIdsAsync(goal);
        var memberUserIds = goal.GroupGoalMembers.Select(m => m.UserId).ToList();

        var books = await db.Books
            .Where(b => bookIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        var unitsByBook = await db.BookUnits
            .Where(u => bookIds.Contains(u.BookId))
            .OrderBy(u => u.SortOrder)
            .GroupBy(u => u.BookId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        var allEntries = await db.GroupProgressEntries
            .Where(pe => pe.GroupGoalId == goal.Id && memberUserIds.Contains(pe.UserId) && bookIds.Contains(pe.BookId))
            .ToListAsync();

        var result = new List<MemberProgressResponse>();

        foreach (var member in goal.GroupGoalMembers)
        {
            foreach (var bookId in bookIds)
            {
                var book = books[bookId];
                var bookUnits = unitsByBook.GetValueOrDefault(bookId) ?? new List<BookUnit>();
                int totalUnits = bookUnits.Count;

                var latestEntry = allEntries
                    .Where(pe => pe.UserId == member.UserId && pe.BookId == bookId)
                    .OrderByDescending(pe => pe.ReportedAt)
                    .FirstOrDefault();

                string currentUnitName = null;
                double progressPercent = 0;

                if (latestEntry != null)
                {
                    var currentUnit = bookUnits.First(u => u.Id == latestEntry.UnitId);
                    currentUnitName = currentUnit.DisplayName;
                    int completed = bookUnits.Count(u => u.SortOrder <= currentUnit.SortOrder);
                    progressPercent = totalUnits > 0 ? Math.Round((double)completed / totalUnits * 100, 1) : 0;
                }

                string expectedUnitName = null;
                if (goal.TargetDate.HasValue && totalUnits > 0)
                {
                    var expectedUnit = CalculateExpectedUnit(goal, bookUnits, totalUnits);
                    if (expectedUnit != null)
                    {
                        expectedUnitName = expectedUnit.DisplayName;
                    }
                }

                result.Add(new MemberProgressResponse
                {
                    UserId = member.UserId,
                    FullName = member.User.FullName,
                    BookId = bookId,
                    BookName = book.Name,
                    CurrentUnitName = currentUnitName,
                    ExpectedUnitName = expectedUnitName,
                    ProgressPercent = progressPercent
                });
            }
        }

        return result;
    }

    private GroupGoalHomeItemResponse BuildGroupGoalHomeItem(
        GroupGoal goal,
        int userId,
        Dictionary<int, int> unitCountsByBook,
        List<GroupProgressEntry> latestEntries,
        Dictionary<int, BookUnit> latestUnits,
        Dictionary<int, List<BookUnit>> allUnitsByBook)
    {
        string scopeName = goal.CategoryId.HasValue
            ? goal.Category?.Name ?? ""
            : string.Join(", ", goal.GroupGoalBooks.Select(ggb => ggb.Book.Name));

        var bookIds = goal.GroupGoalBooks.Select(ggb => ggb.BookId).ToList();

        double progressPercent = 0;
        string currentUnitName = null;
        string expectedUnitName = null;
        int? unitsDelta = null;

        if (bookIds.Count > 0)
        {
            var bookId = bookIds[0];
            int totalUnits = unitCountsByBook.GetValueOrDefault(bookId, 0);

            BookUnit currentUnit = null;
            var latestEntry = latestEntries
                .FirstOrDefault(e => e.GroupGoalId == goal.Id && e.BookId == bookId);

            if (latestEntry != null && latestUnits.TryGetValue(latestEntry.UnitId, out currentUnit))
            {
                currentUnitName = currentUnit.DisplayName;
                var bookUnits = allUnitsByBook.GetValueOrDefault(bookId);
                if (bookUnits != null)
                {
                    int completed = bookUnits.Count(u => u.SortOrder <= currentUnit.SortOrder);
                    progressPercent = totalUnits > 0 ? Math.Round((double)completed / totalUnits * 100, 1) : 0;
                }
            }

            BookUnit expectedUnit = null;
            if (goal.TargetDate.HasValue && totalUnits > 0 && allUnitsByBook.TryGetValue(bookId, out var units))
            {
                expectedUnit = CalculateExpectedUnit(goal, units, totalUnits);
                if (expectedUnit != null)
                {
                    expectedUnitName = expectedUnit.DisplayName;
                }
            }

            if (currentUnit != null && expectedUnit != null && allUnitsByBook.TryGetValue(bookId, out var bookUnitList))
            {
                int currentIndex = bookUnitList.Count(u => u.SortOrder <= currentUnit.SortOrder);
                int expectedIndex = bookUnitList.Count(u => u.SortOrder <= expectedUnit.SortOrder);
                unitsDelta = currentIndex - expectedIndex;
            }
        }

        return new GroupGoalHomeItemResponse
        {
            Id = goal.Id,
            GroupId = goal.GroupId,
            GroupName = goal.Group.Name,
            Title = goal.Title,
            IsCategoryGoal = goal.CategoryId.HasValue,
            ScopeName = scopeName,
            TargetDate = goal.TargetDate,
            ProgressPercent = progressPercent,
            CurrentUnitName = currentUnitName,
            ExpectedUnitName = expectedUnitName,
            UnitsDelta = unitsDelta,
            CreatedAt = goal.CreatedAt
        };
    }

    private static BookUnit CalculateExpectedUnit(GroupGoal goal, List<BookUnit> bookUnits, int totalUnits)
    {
        var goalStartDate  = goal.CreatedAt.Date;
        var targetDateTime = goal.TargetDate.Value.ToDateTime(TimeOnly.MinValue);
        double totalDays   = Math.Max(1, (targetDateTime - goalStartDate).TotalDays);
        double daysElapsed = (DateTime.UtcNow - goal.CreatedAt).TotalDays;
        double ratio       = Math.Min(1, daysElapsed / totalDays);
        int expectedIndex  = Math.Max(1, (int)(ratio * totalUnits));

        return bookUnits
            .OrderBy(u => u.SortOrder)
            .Skip(expectedIndex - 1)
            .FirstOrDefault();
    }

    private async Task<List<int>> GetGoalBookIdsAsync(GroupGoal goal)
    {
        if (goal.CategoryId.HasValue)
        {
            return await db.Books
                .Where(b => b.CategoryId == goal.CategoryId.Value)
                .Select(b => b.Id)
                .ToListAsync();
        }

        return goal.GroupGoalBooks.Select(ggb => ggb.BookId).ToList();
    }

    private async Task<string> GetScopeNameAsync(GroupGoal goal)
    {
        if (goal.CategoryId.HasValue)
        {
            var category = await db.Categories.FirstAsync(c => c.Id == goal.CategoryId.Value);
            return category.Name;
        }
        
        var bookNames = await db.GroupGoalBooks
            .Where(ggb => ggb.GroupGoalId == goal.Id)
            .Select(ggb => ggb.Book.Name)
            .ToListAsync();

        return string.Join(", ", bookNames);
    }
}
