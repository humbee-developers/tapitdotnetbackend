using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.DatingProfile.Commands;

public record UpdateLifestyleCommand(string[] Lifestyle) : IRequest<Result<DatingProfileDto>>;

public class UpdateLifestyleCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateLifestyleCommand, Result<DatingProfileDto>>
{
    public async Task<Result<DatingProfileDto>> Handle(UpdateLifestyleCommand cmd, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct);

        if (profile is null)
        {
            profile = UserDatingProfile.Create(currentUser.UserId!, "", "", [], "", 0, 0, [], cmd.Lifestyle, [], null);
            await uow.Repository<UserDatingProfile>().AddAsync(profile, ct);
        }
        else
        {
            profile.UpdateLifestyle(cmd.Lifestyle);
        }

        await uow.SaveChangesAsync(ct);
        return Result<DatingProfileDto>.Success(ProfileDtoMapper.Map(profile));
    }
}
