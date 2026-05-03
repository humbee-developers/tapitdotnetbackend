using AutoMapper;
using TapitAI.Application.DTOs.Media;
using TapitAI.Domain.Entities;

namespace TapitAI.Application.Mappings;

public class MediaMappingProfile : Profile
{
    public MediaMappingProfile()
    {
        CreateMap<Media, MediaDto>();
    }
}
