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
        builder.HasIndex(n => new { n.UserId, n.Status }).HasDatabaseName("ix_user_notification_user_status");
    }
}
