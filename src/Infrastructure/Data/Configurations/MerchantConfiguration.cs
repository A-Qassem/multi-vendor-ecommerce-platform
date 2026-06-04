using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Infrastructure.Data.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(m => m.Email)
            .IsUnique();

        builder.Property(m => m.Password)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasMany(m => m.RefreshTokens)
            .WithOne(r => r.Merchant)
            .HasForeignKey(r => r.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Products)
            .WithOne(p => p.Merchant)
            .HasForeignKey(p => p.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
