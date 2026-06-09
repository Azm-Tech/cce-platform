using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class ReplyVoteConfiguration : IEntityTypeConfiguration<ReplyVote>
{
    public void Configure(EntityTypeBuilder<ReplyVote> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.HasIndex(v => new { v.ReplyId, v.UserId }).IsUnique().HasDatabaseName("ux_reply_vote_reply_user");
    }
}
