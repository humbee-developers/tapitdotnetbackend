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
        Gender = p.Gender,
        GenderPreference = p.GenderPreference,
        AgeRange = p.AgeRange,
        HeightFt = p.HeightFt,
        HeightIn = p.HeightIn,
        HeightPreference = p.HeightPreference,
        Lifestyle = p.Lifestyle,
        LookingFor = p.LookingFor,
        Bio = p.Bio,
        PrimaryPhotoId = p.PrimaryPhotoId,
        PrimaryPhotoUrl = p.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
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
