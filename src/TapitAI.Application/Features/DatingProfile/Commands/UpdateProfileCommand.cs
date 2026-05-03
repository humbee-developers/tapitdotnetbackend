using MediatR;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record UpdateProfileCommand(
    string DisplayName,
    Guid AgeRangeOptionId,
    Guid SelfGenderOptionId,
    int HeightFt,
    int HeightInch,
    Guid? PreferHeightOptionId,
    string? Description,
    List<Guid> InterestedGenderIds,
    List<Guid> LifestyleIds,
    List<Guid> LookingForIds
) : IRequest<Result<DatingProfileDto>>;

public class UpdateProfileCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateProfileCommand, Result<DatingProfileDto>>
{
    public async Task<Result<DatingProfileDto>> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.AgeRangeOption)
            .Include(p => p.SelfGenderOption)
            .Include(p => p.PreferHeightOption)
            .Include(p => p.InterestedGenders)
            .Include(p => p.Lifestyles)
            .Include(p => p.LookingFors)
            .Include(p => p.Photos)
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("DatingProfile", currentUser.UserId!);

        profile.Update(cmd.DisplayName, cmd.AgeRangeOptionId, cmd.SelfGenderOptionId,
            cmd.HeightFt, cmd.HeightInch, cmd.PreferHeightOptionId, cmd.Description);

        profile.InterestedGenders.Clear();
        profile.Lifestyles.Clear();
        profile.LookingFors.Clear();

        foreach (var id in cmd.InterestedGenderIds)
        {
            var opt = await uow.Repository<LookupOption>().GetByIdAsync(id, ct)
                      ?? throw new NotFoundException(nameof(LookupOption), id);
            profile.InterestedGenders.Add(opt);
        }
        foreach (var id in cmd.LifestyleIds)
        {
            var opt = await uow.Repository<LookupOption>().GetByIdAsync(id, ct)
                      ?? throw new NotFoundException(nameof(LookupOption), id);
            profile.Lifestyles.Add(opt);
        }
        foreach (var id in cmd.LookingForIds)
        {
            var opt = await uow.Repository<LookupOption>().GetByIdAsync(id, ct)
                      ?? throw new NotFoundException(nameof(LookupOption), id);
            profile.LookingFors.Add(opt);
        }

        await uow.SaveChangesAsync(ct);
        return Result<DatingProfileDto>.Success(ProfileDtoMapper.Map(profile));
    }
}
