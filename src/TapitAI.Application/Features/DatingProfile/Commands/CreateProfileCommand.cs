using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record CreateProfileCommand(
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

public class CreateProfileCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateProfileCommand, Result<DatingProfileDto>>
{
    public async Task<Result<DatingProfileDto>> Handle(CreateProfileCommand cmd, CancellationToken ct)
    {
        var existing = await uow.Repository<UserDatingProfile>()
            .Query()
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct);

        if (existing is not null)
            return Result<DatingProfileDto>.Failure("Profile already exists. Use update instead.");

        var profile = UserDatingProfile.Create(
            currentUser.UserId!, cmd.DisplayName,
            cmd.AgeRangeOptionId, cmd.SelfGenderOptionId,
            cmd.HeightFt, cmd.HeightInch,
            cmd.PreferHeightOptionId, cmd.Description);

        await AttachOptions(profile, cmd.InterestedGenderIds, cmd.LifestyleIds, cmd.LookingForIds, ct);

        await uow.Repository<UserDatingProfile>().AddAsync(profile, ct);
        await uow.SaveChangesAsync(ct);

        return Result<DatingProfileDto>.Success(MapToDto(profile));
    }

    private async Task AttachOptions(UserDatingProfile profile,
        List<Guid> genderIds, List<Guid> lifestyleIds, List<Guid> lookingForIds,
        CancellationToken ct)
    {
        foreach (var id in genderIds)
        {
            var opt = await uow.Repository<LookupOption>().GetByIdAsync(id, ct)
                      ?? throw new NotFoundException(nameof(LookupOption), id);
            profile.InterestedGenders.Add(opt);
        }
        foreach (var id in lifestyleIds)
        {
            var opt = await uow.Repository<LookupOption>().GetByIdAsync(id, ct)
                      ?? throw new NotFoundException(nameof(LookupOption), id);
            profile.Lifestyles.Add(opt);
        }
        foreach (var id in lookingForIds)
        {
            var opt = await uow.Repository<LookupOption>().GetByIdAsync(id, ct)
                      ?? throw new NotFoundException(nameof(LookupOption), id);
            profile.LookingFors.Add(opt);
        }
    }

    private static DatingProfileDto MapToDto(UserDatingProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        DisplayName = p.DisplayName,
        AgeRangeOptionId = p.AgeRangeOptionId,
        SelfGenderOptionId = p.SelfGenderOptionId,
        HeightFt = p.HeightFt,
        HeightInch = p.HeightInch,
        PreferHeightOptionId = p.PreferHeightOptionId,
        Description = p.Description,
        PrimaryPhotoId = p.PrimaryPhotoId
    };
}
