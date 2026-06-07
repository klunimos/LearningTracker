using LearningTracker.Api.Data;
using LearningTracker.Api.Data.Entities;
using LearningTracker.Api.Logic.DTO.Group;
using Microsoft.EntityFrameworkCore;

using static LearningTracker.Api.Logic.DTO.Group.GroupRoles;

namespace LearningTracker.Api.Logic.Services;

public interface IGroupService
{
    Task<List<GroupSummaryResponse>> GetMyGroupsAsync(int userId);
    Task<(GroupDetailResponse response, CreateGroupStatus status)> CreateGroupAsync(int userId, CreateGroupRequest request);
    Task<(GroupDetailResponse response, JoinGroupStatus status)> JoinGroupAsync(int userId, JoinGroupRequest request);
    Task<(GroupDetailResponse response, UpdateGroupSettingsStatus status)> UpdateSettingsAsync(int userId, UpdateGroupSettingsRequest request);
    Task<(GroupDetailResponse response, bool found)> GetGroupDetailAsync(int userId, int groupId);
    Task<List<GroupSummaryResponse>> SearchGroupsAsync(int userId, string query, int take = 20);
}

public class GroupService : IGroupService
{
    private readonly AppDbContext db;

    public GroupService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<GroupSummaryResponse>> GetMyGroupsAsync(int userId)
    {
        var memberships = await db.GroupMembers
            .Include(gm => gm.Group).ThenInclude(g => g.Members)
            .Include(gm => gm.Group).ThenInclude(g => g.GroupGoals)
            .Where(gm => gm.UserId == userId)
            .ToListAsync();

        var result = new List<GroupSummaryResponse>();
        foreach (var membership in memberships)
        {
            result.Add(BuildGroupSummary(membership.Group, membership.Role));
        }
        return result;
    }

    public async Task<(GroupDetailResponse response, CreateGroupStatus status)> CreateGroupAsync(int userId, CreateGroupRequest request)
    {
        string inviteCode = request.InviteCode?.Trim() ?? string.Empty;
        if (!IsValidInviteCode(inviteCode))
            return (null, CreateGroupStatus.InvalidInviteCode);

        bool inviteCodeExists = await db.Groups.AnyAsync(g => g.InviteCode == inviteCode);
        if (inviteCodeExists)
            return (null, CreateGroupStatus.InviteCodeAlreadyExists);

        var group = new Group
        {
            Name = request.Name,
            Description = request.Description,
            ProfilePicture = string.IsNullOrWhiteSpace(request.ProfilePicture) ? null : request.ProfilePicture,
            IsPublic = request.IsPublic,
            InviteCode = inviteCode,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            Members = new List<GroupMember>
            {
                new GroupMember
                {
                    UserId = userId,
                    Role = GroupRoles.Admin,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync();

        await db.Entry(group).Collection(g => g.Members).Query()
            .Include(m => m.User)
            .LoadAsync();

        var response = BuildGroupDetail(group, Admin);
        return (response, CreateGroupStatus.Success);
    }

    public async Task<(GroupDetailResponse response, JoinGroupStatus status)> JoinGroupAsync(int userId, JoinGroupRequest request)
    {
        var group = await db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.InviteCode == request.InviteCode);

        if (group == null)
            return (null, JoinGroupStatus.NotFound);

        bool alreadyMember = group.Members.Any(m => m.UserId == userId);
        if (alreadyMember)
            return (null, JoinGroupStatus.AlreadyMember);

        var newMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            Role = Member,
            JoinedAt = DateTime.UtcNow
        };
        db.GroupMembers.Add(newMember);
        await db.SaveChangesAsync();

        await db.Entry(newMember).Reference(m => m.User).LoadAsync();

        var response = BuildGroupDetail(group, Member);
        return (response, JoinGroupStatus.Success);
    }

    public async Task<(GroupDetailResponse response, UpdateGroupSettingsStatus status)> UpdateSettingsAsync(int userId, UpdateGroupSettingsRequest request)
    {
        var group = await db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId);
        if (group == null)
            return (null, UpdateGroupSettingsStatus.GroupNotFound);

        bool isAdmin = group.Members.Any(m => m.UserId == userId && m.Role == Admin);
        if (!isAdmin)
            return (null, UpdateGroupSettingsStatus.NotGroupAdmin);

        string inviteCode = request.InviteCode?.Trim() ?? string.Empty;
        if (!IsValidInviteCode(inviteCode))
            return (null, UpdateGroupSettingsStatus.InvalidInviteCode);

        bool inviteCodeExists = await db.Groups.AnyAsync(g => g.Id != group.Id && g.InviteCode == inviteCode);
        if (inviteCodeExists)
            return (null, UpdateGroupSettingsStatus.InviteCodeAlreadyExists);

        group.Name = request.Name;
        group.Description = request.Description;
        group.ProfilePicture = string.IsNullOrWhiteSpace(request.ProfilePicture) ? null : request.ProfilePicture;
        group.InviteCode = inviteCode;
        group.IsPublic = request.IsPublic;

        await db.SaveChangesAsync();
        var response = BuildGroupDetail(group, Admin);
        return (response, UpdateGroupSettingsStatus.Success);
    }

    public async Task<(GroupDetailResponse response, bool found)> GetGroupDetailAsync(int userId, int groupId)
    {
        var group = await db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return (null, false);

        var membership = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null && !group.IsPublic)
            return (null, false);

        string myRole = membership?.Role;
        var response = BuildGroupDetail(group, myRole);
        return (response, true);
    }

    public async Task<List<GroupSummaryResponse>> SearchGroupsAsync(int userId, string query, int take = 20)
    {
        var groups = await db.Groups
            .Include(g => g.Members)
            .Include(g => g.GroupGoals)
            .Where(g => g.IsPublic && g.Name.Contains(query))
            .Take(take)
            .ToListAsync();

        return groups.Select(g =>
        {
            var membership = g.Members.FirstOrDefault(m => m.UserId == userId);
            return BuildGroupSummary(g, membership?.Role);
        }).ToList();
    }

    private GroupSummaryResponse BuildGroupSummary(Group group, string myRole)
    {
        return new GroupSummaryResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            ProfilePicture = group.ProfilePicture,
            IsPublic = group.IsPublic,
            InviteCode = myRole == Admin ? group.InviteCode : null,
            MemberCount = group.Members.Count,
            GoalCount = group.GroupGoals.Count,
            MyRole = myRole,
            CreatedAt = group.CreatedAt
        };
    }

    private GroupDetailResponse BuildGroupDetail(Group group, string myRole)
    {
        return new GroupDetailResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            ProfilePicture = group.ProfilePicture,
            IsPublic = group.IsPublic,
            InviteCode = myRole == Admin ? group.InviteCode : null,
            MyRole = myRole,
            Members = group.Members.Select(m => new GroupMemberResponse
            {
                UserId = m.UserId,
                FullName = m.User.FullName,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList(),
            CreatedAt = group.CreatedAt
        };
    }

    private static bool IsValidInviteCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.Length < 8)
            return false;
        return value.All(char.IsDigit);
    }
}
