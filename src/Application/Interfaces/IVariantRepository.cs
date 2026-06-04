using MultiVendor.Ecommerce.Application.DTOs.Variants;
using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Application.Interfaces;

public interface IVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id);
    Task<ProductVariant?> GetByIdWithAttributesAsync(Guid id);
    Task<(List<ProductVariant> Items, int TotalCount)> GetAllByProductAsync(Guid productId, VariantListRequest request);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeVariantId = null);
    Task<ProductVariant> CreateAsync(ProductVariant variant);
    Task UpdateAsync(ProductVariant variant);
    Task DeleteAsync(ProductVariant variant);
}
