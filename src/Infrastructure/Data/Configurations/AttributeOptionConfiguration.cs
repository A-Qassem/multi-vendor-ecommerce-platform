using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Infrastructure.Data.Configurations;

public class AttributeOptionConfiguration : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ProductAttributeId)
            .IsRequired();

        builder.Property(o => o.Value)
            .IsRequired()
            .HasMaxLength(100);

        // Relationship
        builder.HasMany(o => o.VariantAttributeValues)
            .WithOne(v => v.AttributeOption)
            .HasForeignKey(v => v.AttributeOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.ProductAttributeId);
    }
}
