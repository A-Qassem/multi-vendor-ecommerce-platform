using MultiVendor.Ecommerce.Application.Common;
using MultiVendor.Ecommerce.Application.DTOs.Variants;

namespace MultiVendor.Ecommerce.Application.Interfaces;

public interface IVariantService
{
    Task<VariantResponse> GetByIdAsync(Guid productId, Guid variantId);
    Task<PagedResult<VariantResponse>> GetAllAsync(Guid productId, VariantListRequest request);
    Task<VariantResponse> CreateAsync(Guid productId, CreateVariantRequest request);
    Task<VariantResponse> UpdateAsync(Guid productId, Guid variantId, UpdateVariantRequest request);
    Task DeleteAsync(Guid productId, Guid variantId);
}
