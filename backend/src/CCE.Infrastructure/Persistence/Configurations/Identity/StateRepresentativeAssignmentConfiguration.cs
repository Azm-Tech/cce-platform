using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class StateRepresentativeAssignmentConfiguration
    : IEntityTypeConfiguration<StateRepresentativeAssignment>
{
    public void Configure(EntityTypeBuilder<StateRepresentativeAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.HasIndex(a => new { a.UserId, a.CountryId })
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_state_rep_active_user_country");
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_state_rep_user_id");
        builder.HasIndex(a => a.CountryId).HasDatabaseName("ix_state_rep_country_id");
    }
}
