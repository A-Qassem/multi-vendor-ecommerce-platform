using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Infrastructure.Data.Configurations;

public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ProductId)
            .IsRequired();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Relationship
        builder.HasMany(a => a.Options)
            .WithOne(o => o.Attribute)
            .HasForeignKey(o => o.ProductAttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.ProductId);
    }
}
