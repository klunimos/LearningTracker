namespace LearningTracker.Api.Logic.DTO.Group;

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
