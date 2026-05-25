using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RecipientUserId);
        builder.Property(x => x.TemplateCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TemplateId);
        builder.Property(x => x.Channel).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ProviderMessageId).HasMaxLength(256);
        builder.Property(x => x.Error).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.SentOn);
        builder.Property(x => x.FailedOn);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.RecipientUserId, x.Status, x.CreatedOn })
            .HasDatabaseName("ix_notification_log_recipient_status_created");
        builder.HasIndex(x => new { x.TemplateCode, x.Channel })
            .HasDatabaseName("ix_notification_log_template_channel");
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("ix_notification_log_correlation_id");
    }
}
