using System.Text.Json;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using CCE.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CCE.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Spec §5.4. For every <see cref="AuditedAttribute"/>-marked entity entering the
/// <see cref="DbContext"/>'s ChangeTracker in Added/Modified/Deleted state, this interceptor
/// inserts an <see cref="AuditEvent"/> in the same transaction. Diff JSON captures the
/// minimal property delta (full body for Added/Deleted, only changed properties for Modified).
/// </summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly ICurrentUserAccessor _userAccessor;
    private readonly ISystemClock _clock;

    public AuditingInterceptor(ICurrentUserAccessor userAccessor, ISystemClock clock)
    {
        _userAccessor = userAccessor;
        _clock = clock;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var actor = _userAccessor.GetActor();
        var correlationId = _userAccessor.GetCorrelationId();
        var now = _clock.UtcNow;

        var auditEvents = new List<AuditEvent>();
        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }
            var entityType = entry.Entity.GetType();
            if (!IsAudited(entityType)) continue;

            var entityName = entityType.Name;
            var resourceId = TryGetEntityId(entry);
            var diff = BuildDiff(entry);
            var action = $"{entityName}.{entry.State}";
            var resource = resourceId is null
                ? $"{entityName}/?"
                : $"{entityName}/{resourceId}";

            auditEvents.Add(new AuditEvent(
                id: System.Guid.NewGuid(),
                occurredOn: now,
                actor: actor,
                action: action,
                resource: resource,
                correlationId: correlationId,
                diff: diff));
        }

        if (auditEvents.Count > 0)
        {
            ctx.AddRange(auditEvents);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static bool IsAudited(System.Type type)
        => type.GetCustomAttributes(typeof(AuditedAttribute), inherit: true).Length > 0;

    private static object? TryGetEntityId(EntityEntry entry)
    {
        var idProp = entry.Metadata.FindPrimaryKey()?.Properties[0];
        if (idProp is null) return null;
        return entry.Property(idProp.Name).CurrentValue;
    }

    private static string BuildDiff(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in entry.Properties)
                    {
                        dict[p.Metadata.Name] = p.CurrentValue;
                    }
                    return JsonSerializer.Serialize(dict, JsonOptions);
                }
            case EntityState.Deleted:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in entry.Properties)
                    {
                        dict[p.Metadata.Name] = p.OriginalValue;
                    }
                    return JsonSerializer.Serialize(dict, JsonOptions);
                }
            case EntityState.Modified:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in entry.Properties)
                    {
                        if (!p.IsModified) continue;
                        dict[p.Metadata.Name] = new { Old = p.OriginalValue, New = p.CurrentValue };
                    }
                    return JsonSerializer.Serialize(dict, JsonOptions);
                }
            default:
                return "{}";
        }
    }
}
