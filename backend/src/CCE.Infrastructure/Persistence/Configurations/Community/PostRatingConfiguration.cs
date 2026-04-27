using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostRatingConfiguration : IEntityTypeConfiguration<PostRating>
{
    public void Configure(EntityTypeBuilder<PostRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasIndex(r => new { r.PostId, r.UserId }).IsUnique().HasDatabaseName("ux_post_rating_post_user");
    }
}
