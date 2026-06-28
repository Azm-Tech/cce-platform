using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

public sealed class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.ToTable("user_device_token");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.DeviceId).IsRequired().HasMaxLength(128);
        builder.Property(t => t.Token).IsRequired().HasMaxLength(512);
        builder.Property(t => t.Platform).IsRequired().HasMaxLength(16);
        builder.Property(t => t.RegisteredOn).IsRequired();
        builder.Property(t => t.LastSeenOn).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();

        // One row per physical device per user.
        builder.HasIndex(t => new { t.UserId, t.DeviceId }).IsUnique();
        // Fast active-token fetch on every push send.
        builder.HasIndex(t => new { t.UserId, t.IsActive });
        // Fast stale-token deactivation after FCM rejects a token value.
        builder.HasIndex(t => t.Token);
    }
}
