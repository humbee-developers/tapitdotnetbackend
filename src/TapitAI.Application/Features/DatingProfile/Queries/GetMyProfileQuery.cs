using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Exceptions;
using TapitAI.Domain.Interfaces.Repositories;

namespace TapitAI.Application.Features.DatingProfile.Queries;

public record GetMyProfileQuery : IRequest<Result<DatingProfileDto>>;

public class GetMyProfileQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetMyProfileQuery, Result<DatingProfileDto>>
{
    public async Task<Result<DatingProfileDto>> Handle(GetMyProfileQuery _, CancellationToken ct)
    {
        var profile = await uow.Repository<UserDatingProfile>().Query()
            .Include(p => p.Photos)
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("DatingProfile", currentUser.UserId!);

        return Result<DatingProfileDto>.Success(ProfileDtoMapper.Map(profile));
    }
}
