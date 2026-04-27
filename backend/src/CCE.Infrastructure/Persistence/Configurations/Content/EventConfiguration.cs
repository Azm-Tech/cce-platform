using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(e => e.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(e => e.DescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(e => e.DescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(e => e.LocationAr).HasMaxLength(512);
        builder.Property(e => e.LocationEn).HasMaxLength(512);
        builder.Property(e => e.OnlineMeetingUrl).HasMaxLength(2048);
        builder.Property(e => e.FeaturedImageUrl).HasMaxLength(2048);
        builder.Property(e => e.ICalUid).HasMaxLength(256).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.HasIndex(e => e.ICalUid).IsUnique().HasDatabaseName("ux_event_ical_uid");
        builder.HasIndex(e => e.StartsOn).HasDatabaseName("ix_event_starts_on");
        builder.Ignore(e => e.DomainEvents);
    }
}
