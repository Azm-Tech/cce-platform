using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class UserNotificationSettingsConfiguration : IEntityTypeConfiguration<UserNotificationSettings>
{
    public void Configure(EntityTypeBuilder<UserNotificationSettings> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Channel).HasConversion<int>().IsRequired();
        builder.Property(x => x.EventCode).HasMaxLength(64);
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.UpdatedOn).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Channel, x.EventCode })
            .IsUnique()
            .HasDatabaseName("ux_user_notification_settings_user_channel_event");
    }
}
