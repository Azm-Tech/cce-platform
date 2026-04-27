using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class NewsletterSubscriptionConfiguration : IEntityTypeConfiguration<NewsletterSubscription>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscription> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Email).HasMaxLength(320).IsRequired();
        builder.Property(n => n.LocalePreference).HasMaxLength(2).IsRequired();
        builder.Property(n => n.ConfirmationToken).HasMaxLength(64).IsRequired();
        builder.HasIndex(n => n.Email).IsUnique().HasDatabaseName("ux_newsletter_email");
        builder.HasIndex(n => n.ConfirmationToken).HasDatabaseName("ix_newsletter_token");
        builder.Ignore(n => n.DomainEvents);
    }
}
