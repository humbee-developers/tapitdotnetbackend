using MediatR;
using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Application.Common.Models;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Interfaces.Repositories;
using TapitAI.Domain.Interfaces.Services;
using TapStatusEntity = TapitAI.Domain.Entities.TapStatus;

namespace TapitAI.Application.Features.TapStatus.Commands;

public record TapInCommand : IRequest<Result<TapStatusDto>>;

public class TapInCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IRealTimeService realTime)
    : IRequestHandler<TapInCommand, Result<TapStatusDto>>
{
    public async Task<Result<TapStatusDto>> Handle(TapInCommand _, CancellationToken ct)
    {
        var status = await uow.Repository<TapStatusEntity>().Query()
            .FirstOrDefaultAsync(ts => ts.UserId == currentUser.UserId, ct);

        if (status is null)
        {
            status = TapStatusEntity.CreateDefault(currentUser.UserId!);
            await uow.Repository<TapStatusEntity>().AddAsync(status, ct);
        }
        else
        {
            status.TapIn();
        }

        await uow.SaveChangesAsync(ct);

        var dto = new TapStatusDto
        {
            UserId = status.UserId,
            Status = status.Status.ToString(),
            AutoTapInAt = status.AutoTapInAt
        };

        await realTime.SendToUserAsync(currentUser.UserId!, HubEvents.TapStatusChanged, dto, ct);
        return Result<TapStatusDto>.Success(dto);
    }
}
