namespace TapitAI.Application.DTOs.Dating;

public class DatingProfileDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public string[] GenderPreference { get; set; } = [];
    public string AgeRange { get; set; } = null!;
    public int HeightFt { get; set; }
    public int HeightIn { get; set; }
    public string[] HeightPreference { get; set; } = [];
    public string[] Lifestyle { get; set; } = [];
    public string[] LookingFor { get; set; } = [];
    public string? Bio { get; set; }
    public string? PrimaryPhotoUrl { get; set; }
    public Guid? PrimaryPhotoId { get; set; }
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
