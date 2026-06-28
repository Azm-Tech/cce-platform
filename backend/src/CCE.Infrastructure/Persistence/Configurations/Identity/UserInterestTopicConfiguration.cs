using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserInterestTopicConfiguration : IEntityTypeConfiguration<UserInterestTopic>
{
    public void Configure(EntityTypeBuilder<UserInterestTopic> builder)
    {
        builder.HasKey(uit => new { uit.UserId, uit.InterestTopicId });

        builder.HasOne(uit => uit.User)
            .WithMany(u => u.UserInterestTopics)
            .HasForeignKey(uit => uit.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uit => uit.InterestTopic)
            .WithMany()
            .HasForeignKey(uit => uit.InterestTopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
