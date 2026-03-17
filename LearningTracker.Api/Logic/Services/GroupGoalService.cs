using LearningTracker.Api.Data;
using LearningTracker.Api.Data.Entities;
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
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
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
        var participatingGoalIds = await db.GroupGoalMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupGoalId)
            .ToListAsync();

        var result = new List<GroupGoalHomeItemResponse>();
        foreach (var goalId in participatingGoalIds)
        {
            var goal = await db.GroupGoals
                .Include(g => g.GroupGoalBooks).ThenInclude(ggb => ggb.Book)
                .FirstOrDefaultAsync(g => g.Id == goalId);
            if (goal == null)
                continue;

            result.Add(await BuildGroupGoalHomeItemAsync(goal, userId));
        }
        return result.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public async Task<(GroupGoalDetailResponse response, CreateGroupGoalStatus status)> CreateGroupGoalAsync(int userId, CreateGroupGoalRequest request)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId);
        if (group == null)
            return (null, CreateGroupGoalStatus.GroupNotFound);

        bool isAdmin = group.Members.Any(m => m.UserId == userId && m.Role == "Admin");
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
            CreatedAt = DateTime.UtcNow
        };

        db.GroupGoals.Add(goal);
        await db.SaveChangesAsync();

        if (!isCategoryGoal)
        {
            foreach (var bookId in request.BookIds)
            {
                db.GroupGoalBooks.Add(new GroupGoalBook { GroupGoalId = goal.Id, BookId = bookId });
            }
            await db.SaveChangesAsync();
        }

        var response = await BuildGoalDetailAsync(goal, false);
        return (response, CreateGroupGoalStatus.Success);
    }

    public async Task<(GroupGoalDetailResponse response, JoinGroupGoalStatus status)> JoinGroupGoalAsync(int userId, JoinGroupGoalRequest request)
    {
        var goal = await db.GroupGoals.FirstOrDefaultAsync(g => g.Id == request.GroupGoalId);
        if (goal == null)
            return (null, JoinGroupGoalStatus.GoalNotFound);

        bool isGroupMember = goal.Group.Members.Any(m => m.UserId == userId);
        if (!isGroupMember)
            return (null, JoinGroupGoalStatus.NotGroupMember);

        bool alreadyParticipating = goal.GroupGoalMembers.Any(m => m.UserId == userId);
        if (alreadyParticipating)
            return (null, JoinGroupGoalStatus.AlreadyParticipating);

        db.GroupGoalMembers.Add(new GroupGoalMember
        {
            GroupGoalId = goal.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var response = await BuildGoalDetailAsync(goal, true);
        return (response, JoinGroupGoalStatus.Success);
    }

    public async Task<(GroupGoalDetailResponse response, ReportGroupProgressStatus status)> ReportProgressAsync(int userId, ReportGroupProgressRequest request)
    {
        var goal = await db.GroupGoals.FirstOrDefaultAsync(g => g.Id == request.GroupGoalId);
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
        var goal = await db.GroupGoals.FirstOrDefaultAsync(g => g.Id == groupGoalId);
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
        List<int> bookIds;
        if (goal.CategoryId.HasValue)
        {
            bookIds = await db.Books
                .Where(b => b.CategoryId == goal.CategoryId.Value)
                .Select(b => b.Id)
                .ToListAsync();
        }
        else
        {
            bookIds = goal.GroupGoalBooks.Select(ggb => ggb.BookId).ToList();
        }

        var result = new List<MemberProgressResponse>();

        foreach (var member in goal.GroupGoalMembers)
        {
            foreach (var bookId in bookIds)
            {
                var book = await db.Books.FirstAsync(b => b.Id == bookId);
                int totalUnits = await db.BookUnits.CountAsync(u => u.BookId == bookId);

                var latestEntry = await db.GroupProgressEntries
                    .Where(pe => pe.GroupGoalId == goal.Id && pe.UserId == member.UserId && pe.BookId == bookId)
                    .OrderByDescending(pe => pe.ReportedAt)
                    .FirstOrDefaultAsync();

                string currentUnitName = null;
                double progressPercent = 0;

                if (latestEntry != null)
                {
                    var currentUnit = await db.BookUnits.FirstAsync(u => u.Id == latestEntry.UnitId);
                    currentUnitName = currentUnit.DisplayName;

                    int completed = await db.BookUnits
                        .CountAsync(u => u.BookId == bookId && u.SortOrder <= currentUnit.SortOrder);

                    progressPercent = totalUnits > 0 ? Math.Round((double)completed / totalUnits * 100, 1) : 0;
                }

                string expectedUnitName = null;
                if (goal.TargetDate.HasValue && totalUnits > 0)
                {
                    var goalStartDate = goal.CreatedAt.Date;
                    var targetDateTime = goal.TargetDate.Value.ToDateTime(TimeOnly.MinValue);
                    double totalDays = (targetDateTime - goalStartDate).TotalDays;
                    if (totalDays < 1)
                        totalDays = 1;
                    double daysElapsed = (DateTime.UtcNow - goal.CreatedAt).TotalDays;
                    double ratio = Math.Min(1, daysElapsed / totalDays);
                    int expectedIndex = (int)(ratio * totalUnits);
                    if (expectedIndex < 1)
                        expectedIndex = 1;
                    var expectedUnit = await db.BookUnits
                        .Where(u => u.BookId == bookId)
                        .OrderBy(u => u.SortOrder)
                        .Skip(expectedIndex - 1)
                        .FirstOrDefaultAsync();
                    if (expectedUnit != null)
                        expectedUnitName = expectedUnit.DisplayName;
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

    private async Task<GroupGoalHomeItemResponse> BuildGroupGoalHomeItemAsync(GroupGoal goal, int userId)
    {
        var group = await db.Groups.FirstAsync(g => g.Id == goal.GroupId);
        string scopeName = await GetScopeNameAsync(goal);

        List<int> bookIds;
        if (goal.CategoryId.HasValue)
        {
            bookIds = await db.Books
                .Where(b => b.CategoryId == goal.CategoryId.Value)
                .Select(b => b.Id)
                .ToListAsync();
        }
        else
        {
            bookIds = goal.GroupGoalBooks.Select(ggb => ggb.BookId).ToList();
        }

        double progressPercent = 0;
        string currentUnitName = null;
        string expectedUnitName = null;
        int? unitsDelta = null;

        if (bookIds.Count > 0)
        {
            var bookId = bookIds[0];
            int totalUnits = await db.BookUnits.CountAsync(u => u.BookId == bookId);

            Data.Entities.BookUnit currentUnit = null;
            var latestEntry = await db.GroupProgressEntries
                .Where(pe => pe.GroupGoalId == goal.Id && pe.UserId == userId && pe.BookId == bookId)
                .OrderByDescending(pe => pe.ReportedAt)
                .FirstOrDefaultAsync();

            if (latestEntry != null)
            {
                currentUnit = await db.BookUnits.FirstAsync(u => u.Id == latestEntry.UnitId);
                currentUnitName = currentUnit.DisplayName;
                int completed = await db.BookUnits
                    .CountAsync(u => u.BookId == bookId && u.SortOrder <= currentUnit.SortOrder);
                progressPercent = totalUnits > 0 ? Math.Round((double)completed / totalUnits * 100, 1) : 0;
            }

            Data.Entities.BookUnit expectedUnit = null;
            if (goal.TargetDate.HasValue && totalUnits > 0)
            {
                var goalStartDate = goal.CreatedAt.Date;
                var targetDateTime = goal.TargetDate.Value.ToDateTime(TimeOnly.MinValue);
                double totalDays = (targetDateTime - goalStartDate).TotalDays;
                if (totalDays < 1)
                    totalDays = 1;
                double daysElapsed = (DateTime.UtcNow - goal.CreatedAt).TotalDays;
                double ratio = Math.Min(1, daysElapsed / totalDays);
                int expectedIndex = (int)(ratio * totalUnits);
                if (expectedIndex < 1)
                    expectedIndex = 1;
                expectedUnit = await db.BookUnits
                    .Where(u => u.BookId == bookId)
                    .OrderBy(u => u.SortOrder)
                    .Skip(expectedIndex - 1)
                    .FirstOrDefaultAsync();
                if (expectedUnit != null)
                    expectedUnitName = expectedUnit.DisplayName;
            }

            if (currentUnit != null && expectedUnit != null)
            {
                int currentIndex = await db.BookUnits.CountAsync(u => u.BookId == bookId && u.SortOrder <= currentUnit.SortOrder);
                int expectedIndex = await db.BookUnits.CountAsync(u => u.BookId == bookId && u.SortOrder <= expectedUnit.SortOrder);
                unitsDelta = currentIndex - expectedIndex;
            }
        }

        return new GroupGoalHomeItemResponse
        {
            Id = goal.Id,
            GroupId = goal.GroupId,
            GroupName = group.Name,
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
