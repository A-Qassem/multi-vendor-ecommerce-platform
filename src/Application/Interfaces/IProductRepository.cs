using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<(List<Product> Items, int TotalCount)> GetAllAsync(Guid merchantId, ProductListRequest request);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
}
