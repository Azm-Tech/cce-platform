using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class CommunityFollowConfiguration : IEntityTypeConfiguration<CommunityFollow>
{
    public void Configure(EntityTypeBuilder<CommunityFollow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.HasIndex(f => new { f.CommunityId, f.UserId }).IsUnique()
            .HasDatabaseName("ux_community_follow_community_user");
    }
}
