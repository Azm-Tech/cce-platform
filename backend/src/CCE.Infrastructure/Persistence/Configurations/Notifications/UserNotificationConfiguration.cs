using System.Collections.Generic;
using System.Text.Json;
using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.RenderedSubjectAr).HasMaxLength(512);
        builder.Property(n => n.RenderedSubjectEn).HasMaxLength(512);
        builder.Property(n => n.RenderedBody).HasColumnType("nvarchar(max)");
        builder.Property(n => n.RenderedLocale).HasMaxLength(2).IsRequired();
        builder.Property(n => n.Channel).HasConversion<int>();
        builder.Property(n => n.Status).HasConversion<int>();

        // Phase 2.2 — actor + deeplink context for the bell/toast push payload.
        // ActorId is nullable (system notifications have no actor). MetaData is a small
        // string→string map persisted as a JSON column via an explicit value converter
        // (EF Core 8's primitive-collection mapping isn't supported on the tooling in
        // service today, so we serialize manually here).
        builder.Property(n => n.ActorId).IsRequired(false);
        builder.Property(n => n.MetaData)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new Dictionary<string, string>(), (JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v)
                    ? new()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                        ?? new Dictionary<string, string>());

        builder.HasIndex(n => new { n.UserId, n.Status }).HasDatabaseName("ix_user_notification_user_status");
    }
}