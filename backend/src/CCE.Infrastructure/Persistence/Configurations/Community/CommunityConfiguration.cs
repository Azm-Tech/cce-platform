using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class CommunityConfiguration : IEntityTypeConfiguration<CCE.Domain.Community.Community>
{
    public void Configure(EntityTypeBuilder<CCE.Domain.Community.Community> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.NameAr).HasMaxLength(CCE.Domain.Community.Community.MaxNameLength).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(CCE.Domain.Community.Community.MaxNameLength).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(160).IsRequired();
        builder.Property(c => c.Visibility).HasConversion<int>();
        builder.Property(c => c.PostCount).HasDefaultValue(0);
        builder.Property(c => c.FollowerCount).HasDefaultValue(0);
        builder.HasIndex(c => c.Slug).IsUnique()
            .HasFilter("[is_deleted] = 0").HasDatabaseName("ux_community_slug_active");
        builder.Ignore(c => c.DomainEvents);
    }
}
