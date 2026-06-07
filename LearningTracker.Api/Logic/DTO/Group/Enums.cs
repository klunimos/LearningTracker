namespace LearningTracker.Api.Logic.DTO.Group;

public static class GroupRoles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
}

public enum CreateGroupStatus
{
    Success,
    InvalidInviteCode,
    InviteCodeAlreadyExists
}

public enum JoinGroupStatus
{
    Success,
    NotFound,
    AlreadyMember
}

public enum UpdateGroupSettingsStatus
{
    Success,
    GroupNotFound,
    NotGroupAdmin,
    InvalidInviteCode,
    InviteCodeAlreadyExists
}
