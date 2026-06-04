using MultiVendor.Ecommerce.Application.Common;
using MultiVendor.Ecommerce.Application.DTOs.Products;

namespace MultiVendor.Ecommerce.Application.Interfaces;

public interface IProductService
{
    Task<ProductResponse> GetByIdAsync(Guid id);
    Task<PagedResult<ProductResponse>> GetAllAsync(ProductListRequest request);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request);
    Task DeleteAsync(Guid id);
}
