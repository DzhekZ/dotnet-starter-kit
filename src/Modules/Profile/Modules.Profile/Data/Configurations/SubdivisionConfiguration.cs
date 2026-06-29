using FSH.Modules.Profile.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Profile.Data.Configurations;

public sealed class SubdivisionConfiguration : IEntityTypeConfiguration<Subdivision>
{
    public void Configure(EntityTypeBuilder<Subdivision> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Subdivisions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(160);
        // Filtered unique index — only enforce uniqueness across live rows
        // so a soft-deleted slug doesn't block recreating the same brand.
        builder.HasIndex(x => x.Slug).HasFilter("\"IsDeleted\" = FALSE");
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.TypeSubdivision).HasMaxLength(512);
        builder.HasIndex(x => x.ParentSubdivisionId);
        builder.HasIndex(x => x.IsDeleted);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.Ignore(x => x.DomainEvents);
    }
}
