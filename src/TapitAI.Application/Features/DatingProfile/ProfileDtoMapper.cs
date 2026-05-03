using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;

namespace TapitAI.Application.Features.DatingProfile;

internal static class ProfileDtoMapper
{
    internal static DatingProfileDto Map(UserDatingProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        DisplayName = p.DisplayName,
        AgeRange = p.AgeRangeOption?.Value ?? string.Empty,
        AgeRangeOptionId = p.AgeRangeOptionId,
        SelfGender = p.SelfGenderOption?.Value ?? string.Empty,
        SelfGenderOptionId = p.SelfGenderOptionId,
        HeightFt = p.HeightFt,
        HeightInch = p.HeightInch,
        PreferHeight = p.PreferHeightOption?.Value,
        PreferHeightOptionId = p.PreferHeightOptionId,
        Description = p.Description,
        PrimaryPhotoId = p.PrimaryPhotoId,
        PrimaryPhotoUrl = p.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
        InterestedGenders = p.InterestedGenders.Select(o => new LookupOptionRefDto { Id = o.Id, Value = o.Value }).ToList(),
        Lifestyles = p.Lifestyles.Select(o => new LookupOptionRefDto { Id = o.Id, Value = o.Value }).ToList(),
        LookingFors = p.LookingFors.Select(o => new LookupOptionRefDto { Id = o.Id, Value = o.Value }).ToList(),
        Photos = p.Photos.OrderBy(ph => ph.DisplayOrder).Select(ph => new ProfilePhotoDto
        {
            Id = ph.Id, PublicUrl = ph.PublicUrl, DisplayOrder = ph.DisplayOrder, IsPrimary = ph.IsPrimary
        }).ToList(),
        Videos = p.Videos.OrderBy(v => v.DisplayOrder).Select(v => new ProfileVideoDto
        {
            Id = v.Id, PublicUrl = v.PublicUrl, DisplayOrder = v.DisplayOrder
        }).ToList()
    };
}
