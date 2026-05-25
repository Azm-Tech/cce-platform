using CCE.Domain.PlatformSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.PlatformSettings;

internal sealed class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.OwnsOne(f => f.Question, question =>
        {
            question.Property(q => q.Ar).HasMaxLength(500).IsRequired();
            question.Property(q => q.En).HasMaxLength(500).IsRequired();
        });
        builder.OwnsOne(f => f.Answer, answer =>
        {
            answer.Property(a => a.Ar).HasColumnType("nvarchar(max)").IsRequired();
            answer.Property(a => a.En).HasColumnType("nvarchar(max)").IsRequired();
        });
    }
}
