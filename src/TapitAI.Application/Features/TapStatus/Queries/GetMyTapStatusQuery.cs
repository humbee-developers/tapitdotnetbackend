using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Repositories;
using TapStatusEntity = TapitAI.Domain.Entities.TapStatus;

namespace TapitAI.Application.Features.TapStatus.Queries;

public record GetMyTapStatusQuery : IRequest<Result<TapStatusDto>>;

public class GetMyTapStatusQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetMyTapStatusQuery, Result<TapStatusDto>>
{
    public async Task<Result<TapStatusDto>> Handle(GetMyTapStatusQuery _, CancellationToken ct)
    {
        var status = await uow.Repository<TapStatusEntity>().Query()
            .FirstOrDefaultAsync(ts => ts.UserId == currentUser.UserId, ct);

        if (status is null)
            return Result<TapStatusDto>.Success(new TapStatusDto
            {
                UserId = currentUser.UserId!,
                Status = TapStatusEnum.TapIn.ToString()
            });

        return Result<TapStatusDto>.Success(new TapStatusDto
        {
            UserId = status.UserId,
            Status = status.Status.ToString(),
            AutoTapInAt = status.AutoTapInAt,
            TapOutReason = status.TapOutReason
        });
    }
}
