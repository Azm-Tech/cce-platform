using CCE.Domain.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Surveys;

internal sealed class SearchQueryLogConfiguration : IEntityTypeConfiguration<SearchQueryLog>
{
    public void Configure(EntityTypeBuilder<SearchQueryLog> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();
        builder.Property(q => q.QueryText).HasMaxLength(1000).IsRequired();
        builder.Property(q => q.Locale).HasMaxLength(2).IsRequired();
        builder.HasIndex(q => q.SubmittedOn).HasDatabaseName("ix_search_query_log_submitted_on");
    }
}
