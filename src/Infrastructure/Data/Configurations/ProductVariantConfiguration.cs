using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Infrastructure.Data.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProductId)
            .IsRequired();

        builder.Property(v => v.SKU)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(v => v.SKU)
            .IsUnique();

        builder.Property(v => v.Quantity)
            .IsRequired();

        builder.Property(v => v.LowStockThreshold)
            .IsRequired(false);

        builder.Property(v => v.PriceOverride)
            .IsRequired(false)
            .HasColumnType("decimal(18,2)");

        builder.Property(v => v.CompareAtPrice)
            .IsRequired(false)
            .HasColumnType("decimal(18,2)");

        builder.Property(v => v.DiscountStartDate)
            .IsRequired(false);

        builder.Property(v => v.DiscountEndDate)
            .IsRequired(false);

        builder.Property(v => v.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(v => v.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.DeletedAt)
            .IsRequired(false);

        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .IsRequired();

        // Relationship
        builder.HasMany(v => v.AttributeValues)
            .WithOne(av => av.Variant)
            .HasForeignKey(av => av.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(v => v.ProductId);
        builder.HasIndex(v => v.IsDeleted);

        // Global query filter — soft delete
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
