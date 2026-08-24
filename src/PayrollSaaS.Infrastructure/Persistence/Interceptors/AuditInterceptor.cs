using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Entities.Auth;

namespace PayrollSaaS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes audit_logs from the EF change tracker on every SaveChanges (doc §5.6).
/// Only entities marked with IAuditable are tracked.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserContext? _currentUser;

    public AuditInterceptor(ICurrentUserContext? currentUser = null)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = eventData.Context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAuditable &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var changes = new Dictionary<string, object?>();

            if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties.Where(p => p.IsModified))
                {
                    changes[prop.Metadata.Name] = new
                    {
                        OldValue = prop.OriginalValue?.ToString(),
                        NewValue = prop.CurrentValue?.ToString()
                    };
                }
            }
            else if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.Properties)
                {
                    changes[prop.Metadata.Name] = prop.CurrentValue?.ToString();
                }
            }

            var entityId = (entry.Entity as BaseEntity)?.Id ?? Guid.Empty;

            eventData.Context.Set<AuditLog>().Add(new AuditLog
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = entityId,
                Action = entry.State.ToString(),
                Changes = JsonSerializer.Serialize(changes),
                PerformedBy = _currentUser?.UserId,
                PerformedAt = DateTime.UtcNow
            });
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
