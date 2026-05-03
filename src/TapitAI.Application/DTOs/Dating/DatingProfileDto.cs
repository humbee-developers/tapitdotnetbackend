namespace TapitAI.Application.DTOs.Dating;

public class DatingProfileDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string AgeRange { get; set; } = null!;
    public Guid AgeRangeOptionId { get; set; }
    public string SelfGender { get; set; } = null!;
    public Guid SelfGenderOptionId { get; set; }
    public int HeightFt { get; set; }
    public int HeightInch { get; set; }
    public string? PreferHeight { get; set; }
    public Guid? PreferHeightOptionId { get; set; }
    public string? Description { get; set; }
    public string? PrimaryPhotoUrl { get; set; }
    public Guid? PrimaryPhotoId { get; set; }
    public List<LookupOptionRefDto> InterestedGenders { get; set; } = new();
    public List<LookupOptionRefDto> Lifestyles { get; set; } = new();
    public List<LookupOptionRefDto> LookingFors { get; set; } = new();
    public List<ProfilePhotoDto> Photos { get; set; } = new();
    public List<ProfileVideoDto> Videos { get; set; } = new();
}

public class ProfilePhotoDto
{
    public Guid Id { get; set; }
    public string PublicUrl { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProfileVideoDto
{
    public Guid Id { get; set; }
    public string PublicUrl { get; set; } = null!;
    public int DisplayOrder { get; set; }
}

public class LookupOptionRefDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = null!;
}
