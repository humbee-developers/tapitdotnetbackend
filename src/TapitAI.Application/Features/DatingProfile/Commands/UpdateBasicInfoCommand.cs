using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record UpdateBasicInfoCommand(
    string DisplayName,
    string Gender,
    string[] GenderPreference,
    string AgeRange,
    int HeightFt,
    int HeightIn,
    string[] HeightPreference
) : IRequest<Result<DatingProfileDto>>;

public class UpdateBasicInfoCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateBasicInfoCommand, Result<DatingProfileDto>>
{
    public async Task<Result<DatingProfileDto>> Handle(UpdateBasicInfoCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct);

        if (profile is null)
        {
            profile = UserDatingProfile.Create(
                currentUser.UserId!, cmd.DisplayName, cmd.Gender, cmd.GenderPreference,
                cmd.AgeRange, cmd.HeightFt, cmd.HeightIn, cmd.HeightPreference, [], [], null);
            await uow.Repository<UserDatingProfile>().AddAsync(profile, ct);
        }
        else
        {
            profile.UpdateBasicInfo(
                cmd.DisplayName, cmd.Gender, cmd.GenderPreference,
                cmd.AgeRange, cmd.HeightFt, cmd.HeightIn, cmd.HeightPreference);
        }

        await uow.SaveChangesAsync(ct);
        return Result<DatingProfileDto>.Success(ProfileDtoMapper.Map(profile));
    }
}
