using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class ExpertRequestAttachmentConfiguration : IEntityTypeConfiguration<ExpertRequestAttachment>
{
    public void Configure(EntityTypeBuilder<ExpertRequestAttachment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.AttachmentType).HasConversion<int>();
    }
}
