using Microsoft.EntityFrameworkCore;
using MultiVendor.Ecommerce.Application.DTOs.Variants;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Infrastructure.Data;

namespace MultiVendor.Ecommerce.Infrastructure.Repositories;

public class VariantRepository : IVariantRepository
{
    private readonly AppDbContext _db;

    public VariantRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductVariant?> GetByIdAsync(Guid id)
    {
        return await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<ProductVariant?> GetByIdWithAttributesAsync(Guid id)
    {
        return await _db.ProductVariants
            .Include(v => v.AttributeValues)
                .ThenInclude(av => av.AttributeOption)
                    .ThenInclude(ao => ao.Attribute)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<(List<ProductVariant> Items, int TotalCount)> GetAllByProductAsync(
        Guid productId, VariantListRequest request)
    {
        var pageSize = Math.Min(request.PageSize, 100);

        var query = _db.ProductVariants
            .Include(v => v.AttributeValues)
                .ThenInclude(av => av.AttributeOption)
                    .ThenInclude(ao => ao.Attribute)
            .Where(v => v.ProductId == productId);

        if (request.IsActive.HasValue)
            query = query.Where(v => v.IsActive == request.IsActive.Value);

        query = (request.SortBy?.ToLower(), request.SortDir?.ToLower()) switch
        {
            ("sku",       "asc")  => query.OrderBy(v => v.SKU),
            ("sku",       _)      => query.OrderByDescending(v => v.SKU),
            ("price",     "asc")  => query.OrderBy(v => v.PriceOverride),
            ("price",     _)      => query.OrderByDescending(v => v.PriceOverride),
            ("quantity",  "asc")  => query.OrderBy(v => v.Quantity),
            ("quantity",  _)      => query.OrderByDescending(v => v.Quantity),
            (_,           "asc")  => query.OrderBy(v => v.CreatedAt),
            _                     => query.OrderByDescending(v => v.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeVariantId = null)
    {
        var query = _db.ProductVariants.Where(v => v.SKU == sku);

        if (excludeVariantId.HasValue)
            query = query.Where(v => v.Id != excludeVariantId.Value);

        return await query.AnyAsync();
    }

    public async Task<ProductVariant> CreateAsync(ProductVariant variant)
    {
        await _db.ProductVariants.AddAsync(variant);
        await _db.SaveChangesAsync();
        return variant;
    }

    public async Task UpdateAsync(ProductVariant variant)
    {
        _db.ProductVariants.Update(variant);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(ProductVariant variant)
    {
        variant.IsDeleted = true;
        variant.DeletedAt = DateTime.UtcNow;
        _db.ProductVariants.Update(variant);
        await _db.SaveChangesAsync();
    }
}
