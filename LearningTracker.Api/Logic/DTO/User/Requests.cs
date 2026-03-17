using System.ComponentModel.DataAnnotations;

namespace LearningTracker.Api.Logic.DTO.User;

public class UpdateProfileRequest
{
    [Required(AllowEmptyStrings = false)]
    public string FullName { get; set; }

    public string ProfilePicture { get; set; }
}
