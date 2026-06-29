using FSH.Modules.Profile.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Profile.Data.Configurations;

public sealed class HierarchyConfiguration : IEntityTypeConfiguration<Hierarchy>
{
    public void Configure(EntityTypeBuilder<Hierarchy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Hierarchies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version);
        builder.Property(x => x.TreeId);
        builder.HasIndex(x => x.TreeId).IsUnique();
        builder.Property(x => x.ParentTreeId);
        builder.HasIndex(x => x.ParentTreeId);
        builder.Property(x => x.Parents).HasMaxLength(1024);
        builder.Property(x => x.ParentsInv).HasMaxLength(1024);
        builder.Property(x => x.Level);
        builder.Property(x => x.PersonCode);

        builder.Ignore(x => x.DomainEvents);
    }
}
