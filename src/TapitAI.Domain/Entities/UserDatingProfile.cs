using TapitAI.Domain.Common;

namespace TapitAI.Domain.Entities;

public class UserDatingProfile : AggregateRoot
{
    public string UserId { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public Guid AgeRangeOptionId { get; private set; }
    public Guid SelfGenderOptionId { get; private set; }
    public int HeightFt { get; private set; }
    public int HeightInch { get; private set; }
    public Guid? PreferHeightOptionId { get; private set; }
    public string? Description { get; private set; }
    public Guid? PrimaryPhotoId { get; private set; }

    public LookupOption AgeRangeOption { get; private set; } = null!;
    public LookupOption SelfGenderOption { get; private set; } = null!;
    public LookupOption? PreferHeightOption { get; private set; }

    public ICollection<LookupOption> InterestedGenders { get; private set; } = new List<LookupOption>();
    public ICollection<LookupOption> Lifestyles { get; private set; } = new List<LookupOption>();
    public ICollection<LookupOption> LookingFors { get; private set; } = new List<LookupOption>();

    private readonly List<ProfilePhoto> _photos = new();
    public IReadOnlyList<ProfilePhoto> Photos => _photos.AsReadOnly();

    private readonly List<ProfileVideo> _videos = new();
    public IReadOnlyList<ProfileVideo> Videos => _videos.AsReadOnly();

    private UserDatingProfile() { }

    public static UserDatingProfile Create(
        string userId, string displayName,
        Guid ageRangeOptionId, Guid selfGenderOptionId,
        int heightFt, int heightInch,
        Guid? preferHeightOptionId, string? description)
        => new()
        {
            UserId = userId,
            DisplayName = displayName,
            AgeRangeOptionId = ageRangeOptionId,
            SelfGenderOptionId = selfGenderOptionId,
            HeightFt = heightFt,
            HeightInch = heightInch,
            PreferHeightOptionId = preferHeightOptionId,
            Description = description
        };

    public void Update(
        string displayName, Guid ageRangeOptionId, Guid selfGenderOptionId,
        int heightFt, int heightInch, Guid? preferHeightOptionId, string? description)
    {
        DisplayName = displayName;
        AgeRangeOptionId = ageRangeOptionId;
        SelfGenderOptionId = selfGenderOptionId;
        HeightFt = heightFt;
        HeightInch = heightInch;
        PreferHeightOptionId = preferHeightOptionId;
        Description = description;
    }

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
