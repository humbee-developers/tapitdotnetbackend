using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record UpdateLookingForCommand(string[] LookingFor) : IRequest<Result<DatingProfileDto>>;

public class UpdateLookingForCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateLookingForCommand, Result<DatingProfileDto>>
{
    public async Task<Result<DatingProfileDto>> Handle(UpdateLookingForCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId!;
        UserDatingProfile? profile = null;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            profile = await uow.Repository<UserDatingProfile>().Query()
                .Include(p => p.Photos)
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                profile = UserDatingProfile.Create(userId, "", "", [], "", 0, 0, [], [], cmd.LookingFor, null);
                await uow.Repository<UserDatingProfile>().AddAsync(profile, ct);
            }
            else
            {
                profile.UpdateLookingFor(cmd.LookingFor);
            }

            try
            {
                await uow.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException ex) when (attempt == 0 && IsDuplicateKey(ex))
            {
                uow.ResetChangeTracker();
            }
        }

        return Result<DatingProfileDto>.Success(ProfileDtoMapper.Map(profile!));
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("23505") == true
        || ex.InnerException?.Message.Contains("duplicate key") == true;
}
