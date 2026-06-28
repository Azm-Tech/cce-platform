using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class CommunityJoinRequestConfiguration : IEntityTypeConfiguration<CommunityJoinRequest>
{
    public void Configure(EntityTypeBuilder<CommunityJoinRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.HasIndex(r => new { r.CommunityId, r.Status }).HasDatabaseName("ix_community_join_request_community_status");
        // At most one pending request per (community, user).
        builder.HasIndex(r => new { r.CommunityId, r.UserId }).IsUnique()
            .HasFilter("[status] = 0").HasDatabaseName("ux_community_join_request_pending");
    }
}
