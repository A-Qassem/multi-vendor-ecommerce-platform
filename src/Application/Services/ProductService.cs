using MultiVendor.Ecommerce.Application.Common;
using MultiVendor.Ecommerce.Application.DTOs.Products;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Domain.Enums;
using Serilog;

namespace MultiVendor.Ecommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICurrentMerchantService _currentMerchant;

    public ProductService(IProductRepository repository, ICurrentMerchantService currentMerchant)
    {
        _repository     = repository;
        _currentMerchant = currentMerchant;
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        if (product.MerchantId != _currentMerchant.MerchantId)
        {
            Log.Warning("Merchant {MerchantId} attempted to access product {ProductId} owned by another merchant",
                _currentMerchant.MerchantId, id);
            throw new UnauthorizedAccessException("You do not own this product.");
        }

        return MapToResponse(product);
    }

    public async Task<PagedResult<ProductResponse>> GetAllAsync(ProductListRequest request)
    {
        var (items, totalCount) = await _repository.GetAllAsync(
            _currentMerchant.MerchantId, request);

        var pageSize = Math.Min(request.PageSize, 100);

        return new PagedResult<ProductResponse>
        {
            Data       = items.Select(MapToResponse).ToList(),
            Page       = request.Page,
            PageSize   = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        if (!Enum.TryParse<ProductStatus>(request.Status, ignoreCase: true, out var status))
            throw new InvalidOperationException($"Invalid status value: '{request.Status}'.");

        var merchantId = _currentMerchant.MerchantId;

        var product = new Product
        {
            MerchantId  = merchantId,
            Name        = request.Name,
            Description = request.Description ?? string.Empty,
            Status      = status,
            BasePrice   = request.BasePrice
        };

        var created = await _repository.CreateAsync(product);

        Log.Information("Product created: {ProductId} by merchant: {MerchantId}", created.Id, merchantId);

        return MapToResponse(created);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        if (product.MerchantId != _currentMerchant.MerchantId)
        {
            Log.Warning("Merchant {MerchantId} attempted to access product {ProductId} owned by another merchant",
                _currentMerchant.MerchantId, id);
            throw new UnauthorizedAccessException("You do not own this product.");
        }

        if (request.Name is not null)        product.Name        = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.BasePrice is not null)   product.BasePrice   = request.BasePrice.Value;

        if (request.Status is not null)
        {
            if (!Enum.TryParse<ProductStatus>(request.Status, ignoreCase: true, out var status))
                throw new InvalidOperationException($"Invalid status value: '{request.Status}'.");
            product.Status = status;
        }

        await _repository.UpdateAsync(product);

        Log.Information("Product updated: {ProductId}", id);

        return MapToResponse(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product '{id}' was not found.");

        if (product.MerchantId != _currentMerchant.MerchantId)
        {
            Log.Warning("Merchant {MerchantId} attempted to access product {ProductId} owned by another merchant",
                _currentMerchant.MerchantId, id);
            throw new UnauthorizedAccessException("You do not own this product.");
        }

        await _repository.DeleteAsync(product);

        Log.Information("Product deleted: {ProductId}", id);
    }

    private static ProductResponse MapToResponse(Product product) => new()
    {
        Id          = product.Id,
        MerchantId  = product.MerchantId,
        Name        = product.Name,
        Description = product.Description,
        Status      = product.Status.ToString(),
        BasePrice   = product.BasePrice,
        CreatedAt   = product.CreatedAt,
        UpdatedAt   = product.UpdatedAt
    };
}
