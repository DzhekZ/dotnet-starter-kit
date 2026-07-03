using FSH.Modules.Profile.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Profile.Data.Configurations;

public sealed class ProfileItemConfiguration : IEntityTypeConfiguration<ProfileItem>
{
    public void Configure(EntityTypeBuilder<ProfileItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Profiles");
        builder.HasKey(x => x.Id);
        // Tenant isolation auto-applied by BaseDbContext; the shadow TenantId column makes Sku/Slug
        // unique-per-tenant, so two tenants can share "ABC-001". Opt out via IGlobalEntity.
        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(275);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.MiddleName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PersonnelNumber).IsRequired();
        builder.Property(x => x.CodePerson).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(100);
        builder.Property(x => x.Login).HasMaxLength(50);
        builder.Property(x => x.AdSid).HasMaxLength(50);
        builder.Property(x => x.DateBirth).HasColumnType("datetime2");
        builder.Property(x => x.DateHire).HasColumnType("datetime2");
        builder.Property(x => x.DateDismiss).HasColumnType("datetime2");
        builder.Property(x => x.Sex).IsRequired();
        builder.Property(x => x.IsBoss).IsRequired();
        builder.Property(x => x.TypeEmployment).HasMaxLength(50);
        builder.Property(x => x.Staffing).HasMaxLength(900);
        builder.Property(x => x.City).HasMaxLength(50);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.Property(x => x.PhoneMobile).HasMaxLength(30);
        builder.Property(x => x.PhoneMobileAllowShow).IsRequired();
        builder.Property(x => x.PhoneWork).HasMaxLength(50);
        builder.Property(x => x.Division).HasMaxLength(50);
        builder.Property(x => x.Place).HasMaxLength(50);
        builder.Property(x => x.WtHcmId).HasMaxLength(50);
        builder.Property(x => x.IsDecret).IsRequired();
        builder.Property(x => x.IsMobilization).IsRequired();
        builder.Property(x => x.Subordinates).IsRequired();
        builder.Property(x => x.Information).HasMaxLength(4000);
        builder.Property(x => x.Description).HasMaxLength(4000);

        // Child collection: ProductImage rows cascade-delete with the product. AutoInclude
        // because product reads typically need the cover image and the join is small.
        builder.HasMany(x => x.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Images).AutoInclude();

        // Derived from the Images collection — not a column.
        builder.Ignore(x => x.ThumbnailUrl);

        builder.Property(x => x.PositionId).IsRequired();
        builder.HasIndex(x => x.PositionId);

        builder.Property(x => x.SubdivisionId).IsRequired();
        builder.HasIndex(x => x.SubdivisionId);

        builder.Property(x => x.HierarchyId).IsRequired();
        builder.HasIndex(x => x.HierarchyId);

        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
