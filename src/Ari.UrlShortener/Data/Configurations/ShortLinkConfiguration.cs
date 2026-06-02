using Ari.UrlShortener.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ari.UrlShortener.Data.Configurations;

public sealed class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.ToTable("ShortLinks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.LongUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.ClickCount).IsRequired();

        // Codes must be globally unique — this is what the redirect looks up on.
        builder.HasIndex(x => x.Code).IsUnique();

        // Supports the cleanup job's "active and expired" scan.
        builder.HasIndex(x => new { x.IsActive, x.ExpiresAtUtc });
    }
}
