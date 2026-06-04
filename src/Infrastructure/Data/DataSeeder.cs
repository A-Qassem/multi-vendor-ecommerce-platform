using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Domain.Enums;

namespace MultiVendor.Ecommerce.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Merchants.AnyAsync()) return;

        var merchant1 = CreateMerchant("Alex Johnson",  "alex@shop.com",     "Password123!");
        var merchant2 = CreateMerchant("Sara Williams", "sara@boutique.com", "Password123!");

        await context.Merchants.AddRangeAsync(merchant1, merchant2);
        await context.SaveChangesAsync();

        await SeedMerchant1Async(context, merchant1.Id);
        await SeedMerchant2Async(context, merchant2.Id);
    }

    private static Merchant CreateMerchant(string name, string email, string password) => new()
    {
        Name     = name,
        Email    = email,
        Password = BCrypt.Net.BCrypt.HashPassword(password)
    };

    private static async Task SeedMerchant1Async(AppDbContext context, Guid merchantId)
    {
        var tshirt = new Product
        {
            MerchantId  = merchantId,
            Name        = "Classic Cotton T-Shirt",
            Description = "Premium cotton t-shirt available in multiple colors and sizes.",
            Status      = ProductStatus.Active,
            BasePrice   = 29.99m
        };
        var laptop = new Product
        {
            MerchantId  = merchantId,
            Name        = "ProBook 15 Laptop",
            Description = "High-performance laptop with configurable RAM and storage.",
            Status      = ProductStatus.Active,
            BasePrice   = 899.99m
        };

        await context.Products.AddRangeAsync(tshirt, laptop);
        await context.SaveChangesAsync();

        // ── T-Shirt: Color × Size ─────────────────────────────────────────────
        var color = new ProductAttribute { ProductId = tshirt.Id, Name = "Color" };
        var size  = new ProductAttribute { ProductId = tshirt.Id, Name = "Size" };
        await context.ProductAttributes.AddRangeAsync(color, size);
        await context.SaveChangesAsync();

        var red   = new AttributeOption { ProductAttributeId = color.Id, Value = "Red" };
        var blue  = new AttributeOption { ProductAttributeId = color.Id, Value = "Blue" };
        var white = new AttributeOption { ProductAttributeId = color.Id, Value = "White" };
        var s     = new AttributeOption { ProductAttributeId = size.Id,  Value = "S" };
        var m     = new AttributeOption { ProductAttributeId = size.Id,  Value = "M" };
        var l     = new AttributeOption { ProductAttributeId = size.Id,  Value = "L" };
        await context.AttributeOptions.AddRangeAsync(red, blue, white, s, m, l);
        await context.SaveChangesAsync();

        await context.ProductVariants.AddRangeAsync(
            Variant(tshirt.Id, "TSHIRT-RED-S",   50, 29.99m, red.Id,   s.Id),
            Variant(tshirt.Id, "TSHIRT-RED-M",   40, 29.99m, red.Id,   m.Id),
            Variant(tshirt.Id, "TSHIRT-BLUE-M",  35, 31.99m, blue.Id,  m.Id),
            Variant(tshirt.Id, "TSHIRT-BLUE-L",  25, 31.99m, blue.Id,  l.Id),
            Variant(tshirt.Id, "TSHIRT-WHITE-S", 20, 27.99m, white.Id, s.Id));
        await context.SaveChangesAsync();

        // ── Laptop: RAM × Storage ─────────────────────────────────────────────
        var ram     = new ProductAttribute { ProductId = laptop.Id, Name = "RAM" };
        var storage = new ProductAttribute { ProductId = laptop.Id, Name = "Storage" };
        await context.ProductAttributes.AddRangeAsync(ram, storage);
        await context.SaveChangesAsync();

        var r8   = new AttributeOption { ProductAttributeId = ram.Id,     Value = "8GB" };
        var r16  = new AttributeOption { ProductAttributeId = ram.Id,     Value = "16GB" };
        var r32  = new AttributeOption { ProductAttributeId = ram.Id,     Value = "32GB" };
        var s256 = new AttributeOption { ProductAttributeId = storage.Id, Value = "256GB SSD" };
        var s512 = new AttributeOption { ProductAttributeId = storage.Id, Value = "512GB SSD" };
        var s1tb = new AttributeOption { ProductAttributeId = storage.Id, Value = "1TB SSD" };
        await context.AttributeOptions.AddRangeAsync(r8, r16, r32, s256, s512, s1tb);
        await context.SaveChangesAsync();

        await context.ProductVariants.AddRangeAsync(
            Variant(laptop.Id, "LAPTOP-8-256",  15,  899.99m, r8.Id,  s256.Id),
            Variant(laptop.Id, "LAPTOP-8-512",  12,  999.99m, r8.Id,  s512.Id),
            Variant(laptop.Id, "LAPTOP-16-512",  8, 1199.99m, r16.Id, s512.Id),
            Variant(laptop.Id, "LAPTOP-16-1TB",  5, 1299.99m, r16.Id, s1tb.Id),
            Variant(laptop.Id, "LAPTOP-32-1TB",  3, 1599.99m, r32.Id, s1tb.Id));
        await context.SaveChangesAsync();
    }

    private static async Task SeedMerchant2Async(AppDbContext context, Guid merchantId)
    {
        var products = new Product[]
        {
            new() { MerchantId = merchantId, Name = "Wireless Headphones", Description = "Noise-cancelling headphones.",    Status = ProductStatus.Active, BasePrice = 149.99m },
            new() { MerchantId = merchantId, Name = "Leather Wallet",      Description = "Genuine leather slim wallet.",    Status = ProductStatus.Active, BasePrice =  39.99m },
            new() { MerchantId = merchantId, Name = "Smart Watch",         Description = "Fitness tracking smartwatch.",    Status = ProductStatus.Active, BasePrice = 299.99m },
            new() { MerchantId = merchantId, Name = "Running Shoes",       Description = "Lightweight running shoes.",      Status = ProductStatus.Active, BasePrice =  89.99m },
            new() { MerchantId = merchantId, Name = "Coffee Maker",        Description = "Programmable drip coffee maker.", Status = ProductStatus.Active, BasePrice =  79.99m },
            new() { MerchantId = merchantId, Name = "Yoga Mat",            Description = "Non-slip 6mm yoga mat.",          Status = ProductStatus.Draft,  BasePrice =  24.99m },
            new() { MerchantId = merchantId, Name = "Desk Lamp",           Description = "LED lamp with USB charging.",     Status = ProductStatus.Active, BasePrice =  34.99m },
            new() { MerchantId = merchantId, Name = "Backpack",            Description = "Waterproof hiking backpack.",     Status = ProductStatus.Active, BasePrice =  59.99m },
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    private static ProductVariant Variant(Guid productId, string sku, int qty, decimal price, params Guid[] optionIds) => new()
    {
        ProductId       = productId,
        SKU             = sku,
        Quantity        = qty,
        PriceOverride   = price,
        IsActive        = true,
        AttributeValues = optionIds
            .Select(id => new VariantAttributeValue { AttributeOptionId = id })
            .ToList()
    };
}
