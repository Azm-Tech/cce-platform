using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class ExpertProfileConfiguration : IEntityTypeConfiguration<ExpertProfile>
{
    public void Configure(EntityTypeBuilder<ExpertProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.BioAr).HasMaxLength(2000);
        builder.Property(p => p.BioEn).HasMaxLength(2000);
        builder.Property(p => p.AcademicTitleAr).HasMaxLength(128);
        builder.Property(p => p.AcademicTitleEn).HasMaxLength(128);
        builder.Property(p => p.ExpertiseTags).HasColumnType("nvarchar(max)");
        builder.HasIndex(p => p.UserId)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_expert_profile_active_user");
    }
}
