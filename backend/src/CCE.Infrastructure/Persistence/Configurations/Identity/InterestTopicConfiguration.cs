using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class InterestTopicConfiguration : IEntityTypeConfiguration<InterestTopic>
{
    public void Configure(EntityTypeBuilder<InterestTopic> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(t => t.NameEn).HasMaxLength(256).IsRequired();
    }
}
