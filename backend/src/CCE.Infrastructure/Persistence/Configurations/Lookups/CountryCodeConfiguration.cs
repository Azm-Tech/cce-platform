using CCE.Domain.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Lookups;

internal sealed class CountryCodeConfiguration : IEntityTypeConfiguration<CountryCode>
{
    public void Configure(EntityTypeBuilder<CountryCode> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.OwnsOne(c => c.Name, name =>
        {
            name.Property(n => n.Ar).HasMaxLength(256).IsRequired();
            name.Property(n => n.En).HasMaxLength(256).IsRequired();
        });
        builder.Property(c => c.DialCode).HasMaxLength(16).IsRequired();
        builder.Property(c => c.FlagUrl).HasMaxLength(2048);
        builder.HasIndex(c => c.DialCode)
               .HasDatabaseName("ix_country_code_dial_code");
        builder.Ignore(c => c.DomainEvents);
    }
}
