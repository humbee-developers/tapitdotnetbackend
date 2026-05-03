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

public record TapOutCommand(int DurationMinutes, string? Reason) : IRequest<Result<TapStatusDto>>;

public class TapOutCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IRealTimeService realTime)
    : IRequestHandler<TapOutCommand, Result<TapStatusDto>>
{
    public async Task<Result<TapStatusDto>> Handle(TapOutCommand cmd, CancellationToken ct)
    {
        var status = await uow.Repository<TapStatusEntity>().Query()
            .FirstOrDefaultAsync(ts => ts.UserId == currentUser.UserId, ct);

        if (status is null)
        {
            status = TapStatusEntity.CreateDefault(currentUser.UserId!);
            await uow.Repository<TapStatusEntity>().AddAsync(status, ct);
        }

        var autoTapInAt = DateTime.UtcNow.AddMinutes(cmd.DurationMinutes);
        status.TapOut(autoTapInAt, cmd.Reason);
        await uow.SaveChangesAsync(ct);

        var dto = new TapStatusDto
        {
            UserId = status.UserId,
            Status = status.Status.ToString(),
            AutoTapInAt = status.AutoTapInAt,
            TapOutReason = status.TapOutReason
        };

        await realTime.SendToUserAsync(currentUser.UserId!, HubEvents.TapStatusChanged, dto, ct);
        return Result<TapStatusDto>.Success(dto);
    }
}
