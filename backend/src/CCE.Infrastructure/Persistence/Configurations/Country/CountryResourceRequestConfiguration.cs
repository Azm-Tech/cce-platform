using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryContentRequestConfiguration : IEntityTypeConfiguration<CountryContentRequest>
{
    public void Configure(EntityTypeBuilder<CountryContentRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Kind).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>().HasColumnName("status");
        builder.Property(r => r.ProposedTitleAr).HasMaxLength(512).IsRequired();
        builder.Property(r => r.ProposedTitleEn).HasMaxLength(512).IsRequired();
        builder.Property(r => r.ProposedDescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ProposedDescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(r => r.AdminNotesAr).HasMaxLength(2000);
        builder.Property(r => r.AdminNotesEn).HasMaxLength(2000);

        // Resource-specific (nullable for News/Event)
        builder.Property(r => r.ProposedResourceType).HasConversion<int>().IsRequired(false);
        builder.Property(r => r.ProposedAssetFileId).IsRequired(false);

        // News/Event-specific
        builder.Property(r => r.ProposedTopicId).IsRequired(false);

        // Event-specific
        builder.Property(r => r.ProposedStartsOn).IsRequired(false);
        builder.Property(r => r.ProposedEndsOn).IsRequired(false);
        builder.Property(r => r.ProposedLocationAr).HasMaxLength(512).IsRequired(false);
        builder.Property(r => r.ProposedLocationEn).HasMaxLength(512).IsRequired(false);
        builder.Property(r => r.ProposedOnlineMeetingUrl).HasMaxLength(2048).IsRequired(false);

        builder.HasIndex(r => new { r.CountryId, r.Status, r.Kind })
            .HasDatabaseName("ix_country_content_request_country_status_kind");
        builder.Ignore(r => r.DomainEvents);
    }
}
