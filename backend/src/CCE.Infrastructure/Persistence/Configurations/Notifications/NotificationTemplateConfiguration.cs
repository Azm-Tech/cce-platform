using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.Property(t => t.SubjectAr).HasMaxLength(512).IsRequired();
        builder.Property(t => t.SubjectEn).HasMaxLength(512).IsRequired();
        builder.Property(t => t.BodyAr).HasColumnType("nvarchar(max)");
        builder.Property(t => t.BodyEn).HasColumnType("nvarchar(max)");
        builder.Property(t => t.Channel).HasConversion<int>();
        builder.Property(t => t.VariableSchemaJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("ux_notification_template_code");
    }
}
