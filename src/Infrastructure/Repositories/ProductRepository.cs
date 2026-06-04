using Microsoft.EntityFrameworkCore;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Domain.Enums;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Infrastructure.Data;

namespace MultiVendor.Ecommerce.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(List<Product> Items, int TotalCount)> GetAllAsync(
        Guid merchantId, ProductListRequest request)
    {
        var pageSize = Math.Min(request.PageSize, 100);

        var query = _db.Products.Where(p => p.MerchantId == merchantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search));

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ProductStatus>(request.Status, ignoreCase: true, out var statusEnum))
            query = query.Where(p => p.Status == statusEnum);

        query = (request.SortBy?.ToLower(), request.SortDir?.ToLower()) switch
        {
            ("name",       "asc")  => query.OrderBy(p => p.Name),
            ("name",       _)      => query.OrderByDescending(p => p.Name),
            ("baseprice",  "asc")  => query.OrderBy(p => p.BasePrice),
            ("baseprice",  _)      => query.OrderByDescending(p => p.BasePrice),
            (_,            "asc")  => query.OrderBy(p => p.CreatedAt),
            _                      => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await _db.Products.AddAsync(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }
}
