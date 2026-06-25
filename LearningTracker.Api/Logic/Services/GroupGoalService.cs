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
    Task<List<JoinableGroupGoalResponse>> GetJoinableGoalsAsync(int userId);
    Task<(GroupGoalDetailResponse response, CreateGroupGoalStatus status)> CreateGroupGoalAsync(int userId, CreateGroupGoalRequest request);
    Task<(GroupGoalDetailResponse response, JoinGroupGoalStatus status)> JoinGroupGoalAsync(int userId, JoinGroupGoalRequest request);
    Task<(GroupGoalDetailResponse response, ReportGroupProgressStatus status)> ReportProgressAsync(int userId, ReportGroupProgressRequest request);
    Task<(List<MemberProgressResponse> response, bool found)> GetMembersProgressAsync(int userId, int groupGoalId);
    Task<(GroupGoalPageResponse response, bool found)> GetGoalDetailAsync(int userId, int groupGoalId);
    Task<(GroupGoalPageResponse response, SetCollectiveTargetStatus status)> SetCollectiveTargetAsync(int userId, SetCollectiveTargetRequest request);
    Task<(GroupGoalPageResponse response, SetParticipationActiveStatus status)> SetParticipationActiveAsync(int userId, SetParticipationActiveRequest request);
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
            .Where(g => g.GroupGoalMembers.Any(m => m.UserId == userId && m.IsActive))
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

    public async Task<List<JoinableGroupGoalResponse>> GetJoinableGoalsAsync(int userId)
    {
        // Group goals from groups the user belongs to, but has not joined yet.
        var goals = await db.GroupGoals
            .Include(g => g.Group)
            .Include(g => g.Category)
            .Include(g => g.GroupGoalMembers)
            .Include(g => g.GroupGoalBooks).ThenInclude(ggb => ggb.Book)
            .Where(g => g.Group.Members.Any(m => m.UserId == userId)
                     && !g.GroupGoalMembers.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return goals.Select(g => new JoinableGroupGoalResponse
        {
            Id = g.Id,
            GroupId = g.GroupId,
            GroupName = g.Group.Name,
            Title = g.Title,
            IsCategoryGoal = g.CategoryId.HasValue,
            ScopeName = g.CategoryId.HasValue
                ? g.Category?.Name ?? ""
                : string.Join(", ", g.GroupGoalBooks.Select(ggb => ggb.Book.Name)),
            TargetDate = g.TargetDate,
            MemberCount = g.GroupGoalMembers.Count
        }).ToList();
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
            StartUnitId = request.StartUnitId,
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
            .Include(g => g.GroupGoalMembers).ThenInclude(m => m.User)
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
            JoinedAt = DateTime.UtcNow,
            IsActive = true
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

    public async Task<(GroupGoalPageResponse response, bool found)> GetGoalDetailAsync(int userId, int groupGoalId)
    {
        var goal = await db.GroupGoals
            .Include(g => g.Group).ThenInclude(g => g.Members)
            .Include(g => g.Category)
            .Include(g => g.GroupGoalMembers).ThenInclude(m => m.User)
            .Include(g => g.GroupGoalBooks).ThenInclude(ggb => ggb.Book)
            .FirstOrDefaultAsync(g => g.Id == groupGoalId);
        if (goal == null)
            return (null, false);

        bool isGroupMember = goal.Group.Members.Any(m => m.UserId == userId);
        if (!isGroupMember)
            return (null, false);

        var bookIds = await GetGoalBookIdsAsync(goal);
        var books = await db.Books
            .Where(b => bookIds.Contains(b.Id))
            .Select(b => new GroupGoalBookResponse { BookId = b.Id, BookName = b.Name })
            .ToListAsync();

        var membersProgress = await BuildMembersProgressAsync(goal);

        var unitsByBook = await db.BookUnits
            .Where(u => bookIds.Contains(u.BookId))
            .OrderBy(u => u.SortOrder)
            .GroupBy(u => u.BookId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        var myEntries = await db.GroupProgressEntries
            .Where(pe => pe.GroupGoalId == goal.Id && pe.UserId == userId && bookIds.Contains(pe.BookId))
            .ToListAsync();

        var startUnit = FindStartUnit(goal, unitsByBook);

        var (totalUnits, myCompleted, myCurrentName) = ComputeMyProgress(bookIds, unitsByBook, myEntries, startUnit);
        double myPercent = totalUnits > 0 ? Math.Round((double)myCompleted / totalUnits * 100, 1) : 0;

        var pace = ComputePaceTarget(goal, bookIds, unitsByBook, startUnit);
        var collective = await ComputeCollectiveTargetAsync(goal, unitsByBook, startUnit);

        var response = new GroupGoalPageResponse
        {
            Id = goal.Id,
            GroupId = goal.GroupId,
            GroupName = goal.Group.Name,
            Title = goal.Title,
            IsCategoryGoal = goal.CategoryId.HasValue,
            ScopeName = await GetScopeNameAsync(goal),
            TargetDate = goal.TargetDate,
            IsParticipating = goal.GroupGoalMembers.Any(m => m.UserId == userId),
            IsActive = goal.GroupGoalMembers.FirstOrDefault(m => m.UserId == userId)?.IsActive ?? false,
            IsGroupAdmin = goal.Group.Members.Any(m => m.UserId == userId && m.Role == GroupRoles.Admin),
            Books = books,
            MembersProgress = membersProgress,
            CreatedAt = goal.CreatedAt,
            TotalUnits = totalUnits,
            MyCompletedUnits = myCompleted,
            MyProgressPercent = myPercent,
            MyCurrentUnitName = myCurrentName,
            PaceTargetPercent = pace.percent,
            PaceTargetLabel = pace.label,
            CollectiveTargetPercent = collective.percent,
            CollectiveTargetLabel = collective.label,
            CollectiveTargetUnitId = collective.unitId,
            CollectiveTargetBookId = collective.bookId
        };
        return (response, true);
    }

    public async Task<(GroupGoalPageResponse response, SetCollectiveTargetStatus status)> SetCollectiveTargetAsync(int userId, SetCollectiveTargetRequest request)
    {
        var goal = await db.GroupGoals
            .Include(g => g.Group).ThenInclude(g => g.Members)
            .Include(g => g.GroupGoalBooks)
            .FirstOrDefaultAsync(g => g.Id == request.GroupGoalId);
        if (goal == null)
            return (null, SetCollectiveTargetStatus.GoalNotFound);

        bool isAdmin = goal.Group.Members.Any(m => m.UserId == userId && m.Role == GroupRoles.Admin);
        if (!isAdmin)
            return (null, SetCollectiveTargetStatus.NotGroupAdmin);

        if (request.CollectiveTargetUnitId.HasValue)
        {
            var bookIds = await GetGoalBookIdsAsync(goal);
            bool unitInGoal = await db.BookUnits.AnyAsync(u =>
                u.Id == request.CollectiveTargetUnitId.Value && bookIds.Contains(u.BookId));
            if (!unitInGoal)
                return (null, SetCollectiveTargetStatus.UnitNotInGoal);
        }

        goal.CollectiveTargetUnitId = request.CollectiveTargetUnitId;
        await db.SaveChangesAsync();

        var (response, _) = await GetGoalDetailAsync(userId, goal.Id);
        return (response, SetCollectiveTargetStatus.Success);
    }

    public async Task<(GroupGoalPageResponse response, SetParticipationActiveStatus status)> SetParticipationActiveAsync(int userId, SetParticipationActiveRequest request)
    {
        var member = await db.GroupGoalMembers
            .FirstOrDefaultAsync(m => m.GroupGoalId == request.GroupGoalId && m.UserId == userId);
        if (member == null)
            return (null, SetParticipationActiveStatus.NotParticipating);

        member.IsActive = request.IsActive;
        await db.SaveChangesAsync();

        var (response, found) = await GetGoalDetailAsync(userId, request.GroupGoalId);
        if (!found)
            return (null, SetParticipationActiveStatus.GoalNotFound);
        return (response, SetParticipationActiveStatus.Success);
    }

    /// <summary>The goal's start unit (the final target spans from here to the book end), found within the loaded units.</summary>
    private static BookUnit FindStartUnit(GroupGoal goal, Dictionary<int, List<BookUnit>> unitsByBook)
    {
        if (!goal.StartUnitId.HasValue) return null;
        return unitsByBook.Values.SelectMany(u => u).FirstOrDefault(u => u.Id == goal.StartUnitId.Value);
    }

    /// <summary>The sort order the goal starts counting from in a given book (the start unit if it belongs to that book, otherwise the book's first unit).</summary>
    private static int StartSortOrder(BookUnit startUnit, int bookId, List<BookUnit> bookUnits)
        => (startUnit != null && startUnit.BookId == bookId)
            ? startUnit.SortOrder
            : (bookUnits.Count > 0 ? bookUnits[0].SortOrder : 1);

    /// <summary>Position of a target unit on the [start → book end] scale, as a percentage (0–100).</summary>
    private static double PercentFromStart(List<BookUnit> bookUnits, int startSortOrder, BookUnit target)
    {
        int bookTotal = bookUnits.Count(u => u.SortOrder >= startSortOrder);
        if (bookTotal == 0) return 0;
        int upTo = bookUnits.Count(u => u.SortOrder >= startSortOrder && u.SortOrder <= target.SortOrder);
        return Math.Round((double)upTo / bookTotal * 100, 1);
    }

    /// <summary>My aggregate progress across all goal books, measured from the start unit to the book end (final target).</summary>
    private static (int totalUnits, int completedUnits, string currentUnitName) ComputeMyProgress(
        List<int> bookIds,
        Dictionary<int, List<BookUnit>> unitsByBook,
        List<GroupProgressEntry> myEntries,
        BookUnit startUnit)
    {
        int totalUnits = 0;
        int completedUnits = 0;
        string currentUnitName = null;

        foreach (var bookId in bookIds)
        {
            var bookUnits = unitsByBook.GetValueOrDefault(bookId) ?? new List<BookUnit>();
            int startSort = StartSortOrder(startUnit, bookId, bookUnits);
            totalUnits += bookUnits.Count(u => u.SortOrder >= startSort);

            var latestEntry = myEntries
                .Where(pe => pe.BookId == bookId)
                .OrderByDescending(pe => pe.ReportedAt)
                .FirstOrDefault();
            if (latestEntry == null) continue;

            var currentUnit = bookUnits.FirstOrDefault(u => u.Id == latestEntry.UnitId);
            if (currentUnit == null) continue;

            completedUnits += bookUnits.Count(u => u.SortOrder >= startSort && u.SortOrder <= currentUnit.SortOrder);
            currentUnitName ??= currentUnit.DisplayName;
        }

        return (totalUnits, completedUnits, currentUnitName);
    }

    /// <summary>
    /// Donut marker — where the group is *supposed* to hold by now, derived from the final target date (pace).
    /// </summary>
    private static (double? percent, string label) ComputePaceTarget(
        GroupGoal goal,
        List<int> bookIds,
        Dictionary<int, List<BookUnit>> unitsByBook,
        BookUnit startUnit)
    {
        if (!goal.TargetDate.HasValue)
            return (null, null);

        var goalStartDate = goal.CreatedAt.Date;
        var targetDateTime = goal.TargetDate.Value.ToDateTime(TimeOnly.MinValue);
        double totalDays = Math.Max(1, (targetDateTime - goalStartDate).TotalDays);
        double daysElapsed = (DateTime.UtcNow - goal.CreatedAt).TotalDays;
        double ratio = Math.Min(1, Math.Max(0, daysElapsed / totalDays));
        double percent = Math.Round(ratio * 100, 1);

        string label = null;
        var firstBookId = bookIds.FirstOrDefault();
        if (firstBookId != 0)
        {
            var bookUnits = unitsByBook.GetValueOrDefault(firstBookId) ?? new List<BookUnit>();
            int startSort = StartSortOrder(startUnit, firstBookId, bookUnits);
            var spanUnits = bookUnits.Where(u => u.SortOrder >= startSort).ToList();
            if (spanUnits.Count > 0)
            {
                int expectedIndex = Math.Max(1, (int)(ratio * spanUnits.Count));
                label = spanUnits.Skip(expectedIndex - 1).FirstOrDefault()?.DisplayName;
            }
        }

        return (percent, label);
    }

    /// <summary>
    /// Bottom-bar + report-modal marker — where the group collectively holds (the preset collective target unit).
    /// </summary>
    private async Task<(double? percent, string label, int? unitId, int? bookId)> ComputeCollectiveTargetAsync(
        GroupGoal goal,
        Dictionary<int, List<BookUnit>> unitsByBook,
        BookUnit startUnit)
    {
        if (!goal.CollectiveTargetUnitId.HasValue)
            return (null, null, null, null);

        var targetUnit = await db.BookUnits.FirstOrDefaultAsync(u => u.Id == goal.CollectiveTargetUnitId.Value);
        if (targetUnit == null)
            return (null, null, null, null);

        var bookUnits = unitsByBook.GetValueOrDefault(targetUnit.BookId) ?? new List<BookUnit>();
        if (bookUnits.Count == 0)
            return (null, null, null, null);

        int startSort = StartSortOrder(startUnit, targetUnit.BookId, bookUnits);
        double percent = PercentFromStart(bookUnits, startSort, targetUnit);
        return (percent, targetUnit.DisplayName, targetUnit.Id, targetUnit.BookId);
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

        var startUnit = FindStartUnit(goal, unitsByBook);
        var result = new List<MemberProgressResponse>();

        foreach (var member in goal.GroupGoalMembers)
        {
            foreach (var bookId in bookIds)
            {
                var book = books[bookId];
                var bookUnits = unitsByBook.GetValueOrDefault(bookId) ?? new List<BookUnit>();
                int startSort = StartSortOrder(startUnit, bookId, bookUnits);
                int totalUnits = bookUnits.Count(u => u.SortOrder >= startSort);

                var latestEntry = allEntries
                    .Where(pe => pe.UserId == member.UserId && pe.BookId == bookId)
                    .OrderByDescending(pe => pe.ReportedAt)
                    .FirstOrDefault();

                string currentUnitName = null;
                int currentUnitId = 0;
                double progressPercent = 0;

                if (latestEntry != null)
                {
                    var currentUnit = bookUnits.First(u => u.Id == latestEntry.UnitId);
                    currentUnitName = currentUnit.DisplayName;
                    currentUnitId = currentUnit.Id;
                    int completed = bookUnits.Count(u => u.SortOrder >= startSort && u.SortOrder <= currentUnit.SortOrder);
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
                    CurrentUnitId = currentUnitId,
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
