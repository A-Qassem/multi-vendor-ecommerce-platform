using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Infrastructure.Data.Configurations;

public class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<VariantAttributeValue> builder)
    {
        // Composite primary key
        builder.HasKey(av => new { av.VariantId, av.AttributeOptionId });

        builder.Property(av => av.VariantId)
            .IsRequired();

        builder.Property(av => av.AttributeOptionId)
            .IsRequired();

        // FK to ProductVariant — defined here to avoid duplicate relationship config
        builder.HasOne(av => av.Variant)
            .WithMany(v => v.AttributeValues)
            .HasForeignKey(av => av.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to AttributeOption — defined here to avoid duplicate relationship config
        builder.HasOne(av => av.AttributeOption)
            .WithMany(o => o.VariantAttributeValues)
            .HasForeignKey(av => av.AttributeOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
