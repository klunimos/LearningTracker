using System.ComponentModel.DataAnnotations;

namespace LearningTracker.Api.Logic.DTO.Group;

public class CreateGroupRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }

    public string Description { get; set; }
    public string ProfilePicture { get; set; }

    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^\d{8,}$")]
    public string InviteCode { get; set; }

    public bool IsPublic { get; set; }
}

public class JoinGroupRequest
{
    [Required(AllowEmptyStrings = false)]
    public string InviteCode { get; set; }
}

public class UpdateGroupSettingsRequest
{
    public int GroupId { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }

    public string Description { get; set; }
    public string ProfilePicture { get; set; }

    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^\d{8,}$")]
    public string InviteCode { get; set; }

    public bool IsPublic { get; set; }
}
