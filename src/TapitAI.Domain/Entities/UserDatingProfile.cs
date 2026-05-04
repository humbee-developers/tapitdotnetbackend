using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class UserDatingProfile : AggregateRoot
{
    public string UserId { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string Gender { get; private set; } = null!;
    public string[] GenderPreference { get; private set; } = [];
    public string AgeRange { get; private set; } = null!;
    public int HeightFt { get; private set; }
    public int HeightIn { get; private set; }
    public string[] HeightPreference { get; private set; } = [];
    public string[] Lifestyle { get; private set; } = [];
    public string[] LookingFor { get; private set; } = [];
    public string? Bio { get; private set; }
    public Guid? PrimaryPhotoId { get; private set; }

    private readonly List<ProfilePhoto> _photos = new();
    public IReadOnlyList<ProfilePhoto> Photos => _photos.AsReadOnly();

    private readonly List<ProfileVideo> _videos = new();
    public IReadOnlyList<ProfileVideo> Videos => _videos.AsReadOnly();

    private UserDatingProfile() { }

    public static UserDatingProfile Create(
        string userId, string displayName, string gender, string[] genderPreference,
        string ageRange, int heightFt, int heightIn,
        string[] heightPreference, string[] lifestyle, string[] lookingFor, string? bio)
        => new()
        {
            UserId = userId,
            DisplayName = displayName,
            Gender = gender,
            GenderPreference = genderPreference,
            AgeRange = ageRange,
            HeightFt = heightFt,
            HeightIn = heightIn,
            HeightPreference = heightPreference,
            Lifestyle = lifestyle,
            LookingFor = lookingFor,
            Bio = bio
        };

    public void Update(
        string displayName, string gender, string[] genderPreference,
        string ageRange, int heightFt, int heightIn,
        string[] heightPreference, string[] lifestyle, string[] lookingFor, string? bio)
    {
        DisplayName = displayName;
        Gender = gender;
        GenderPreference = genderPreference;
        AgeRange = ageRange;
        HeightFt = heightFt;
        HeightIn = heightIn;
        HeightPreference = heightPreference;
        Lifestyle = lifestyle;
        LookingFor = lookingFor;
        Bio = bio;
    }

    public void UpdateBasicInfo(
        string displayName, string gender, string[] genderPreference,
        string ageRange, int heightFt, int heightIn, string[] heightPreference)
    {
        DisplayName = displayName;
        Gender = gender;
        GenderPreference = genderPreference;
        AgeRange = ageRange;
        HeightFt = heightFt;
        HeightIn = heightIn;
        HeightPreference = heightPreference;
    }

    public void UpdateLifestyle(string[] lifestyle) => Lifestyle = lifestyle;
    public void UpdateLookingFor(string[] lookingFor) => LookingFor = lookingFor;
    public void UpdateBio(string? bio) => Bio = bio;

    public void SetPrimaryPhoto(Guid? photoId) => PrimaryPhotoId = photoId;

    public void AddPhoto(ProfilePhoto photo) => _photos.Add(photo);

    public void RemovePhoto(ProfilePhoto photo)
    {
        _photos.Remove(photo);
        if (PrimaryPhotoId == photo.Id) PrimaryPhotoId = _photos.FirstOrDefault()?.Id;
    }

    public void AddVideo(ProfileVideo video) => _videos.Add(video);
    public void RemoveVideo(ProfileVideo video) => _videos.Remove(video);
}
