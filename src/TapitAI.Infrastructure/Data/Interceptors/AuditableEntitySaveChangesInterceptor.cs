using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Common;

namespace TapitAI.Infrastructure.Data.Interceptors;

public class AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUser, IDateTimeService dateTime)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUser.UserId;
                entry.Entity.CreatedAt = dateTime.UtcNow;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedBy = currentUser.UserId;
                entry.Entity.UpdatedAt = dateTime.UtcNow;
            }
        }
    }
}
