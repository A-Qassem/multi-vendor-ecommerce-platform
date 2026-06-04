using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Infrastructure.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(i => i.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.HasIndex(i => i.ProductId);
    }
}
