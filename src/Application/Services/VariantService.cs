using MultiVendor.Ecommerce.Application.Common;
using MultiVendor.Ecommerce.Application.DTOs.Variants;
using MultiVendor.Ecommerce.Application.Events;
using MultiVendor.Ecommerce.Application.Interfaces;
using MultiVendor.Ecommerce.Domain.Entities;
using Serilog;

namespace MultiVendor.Ecommerce.Application.Services;

public class VariantService : IVariantService
{
    private readonly IVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentMerchantService _currentMerchant;
    private readonly IEventHandler<LowStockEvent> _lowStockHandler;

    public VariantService(
        IVariantRepository variantRepository,
        IProductRepository productRepository,
        ICurrentMerchantService currentMerchant,
        IEventHandler<LowStockEvent> lowStockHandler)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _currentMerchant   = currentMerchant;
        _lowStockHandler   = lowStockHandler;
    }

    public async Task<VariantResponse> GetByIdAsync(Guid productId, Guid variantId)
    {
        await VerifyProductOwnershipAsync(productId);

        var variant = await _variantRepository.GetByIdWithAttributesAsync(variantId)
            ?? throw new KeyNotFoundException($"Variant '{variantId}' was not found.");

        if (variant.ProductId != productId)
            throw new KeyNotFoundException($"Variant '{variantId}' was not found.");

        return MapToResponse(variant);
    }

    public async Task<PagedResult<VariantResponse>> GetAllAsync(Guid productId, VariantListRequest request)
    {
        await VerifyProductOwnershipAsync(productId);

        var (items, totalCount) = await _variantRepository.GetAllByProductAsync(productId, request);
        var pageSize = Math.Min(request.PageSize, 100);

        return new PagedResult<VariantResponse>
        {
            Data       = items.Select(MapToResponse).ToList(),
            Page       = request.Page,
            PageSize   = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<VariantResponse> CreateAsync(Guid productId, CreateVariantRequest request)
    {
        await VerifyProductOwnershipAsync(productId);

        if (await _variantRepository.SkuExistsAsync(request.SKU))
        {
            Log.Warning("Duplicate SKU attempted: {SKU}", request.SKU);
            throw new InvalidOperationException($"SKU '{request.SKU}' already exists.");
        }

        var variant = new ProductVariant
        {
            ProductId         = productId,
            SKU               = request.SKU,
            Quantity          = request.Quantity,
            LowStockThreshold = request.LowStockThreshold,
            PriceOverride     = request.PriceOverride,
            CompareAtPrice    = request.CompareAtPrice,
            DiscountStartDate = request.DiscountStartDate,
            DiscountEndDate   = request.DiscountEndDate,
            IsActive          = request.IsActive,
            AttributeValues   = request.Attributes
                .Select(a => new VariantAttributeValue { AttributeOptionId = a.AttributeOptionId })
                .ToList()
        };

        var created = await _variantRepository.CreateAsync(variant);
        Log.Information("Variant created: {VariantId} with SKU: {SKU}", created.Id, request.SKU);

        var withAttributes = await _variantRepository.GetByIdWithAttributesAsync(created.Id)
            ?? throw new InvalidOperationException("Failed to load created variant.");

        return MapToResponse(withAttributes);
    }

    public async Task<VariantResponse> UpdateAsync(Guid productId, Guid variantId, UpdateVariantRequest request)
    {
        await VerifyProductOwnershipAsync(productId);

        var variant = await _variantRepository.GetByIdWithAttributesAsync(variantId)
            ?? throw new KeyNotFoundException($"Variant '{variantId}' was not found.");

        if (variant.ProductId != productId)
            throw new KeyNotFoundException($"Variant '{variantId}' was not found.");

        if (request.SKU is not null && request.SKU != variant.SKU)
        {
            if (await _variantRepository.SkuExistsAsync(request.SKU, excludeVariantId: variantId))
            {
                Log.Warning("Duplicate SKU attempted: {SKU}", request.SKU);
                throw new InvalidOperationException($"SKU '{request.SKU}' already exists.");
            }
            variant.SKU = request.SKU;
        }

        if (request.Quantity.HasValue)          variant.Quantity          = request.Quantity.Value;
        if (request.LowStockThreshold.HasValue) variant.LowStockThreshold = request.LowStockThreshold;
        if (request.PriceOverride.HasValue)     variant.PriceOverride     = request.PriceOverride;
        if (request.CompareAtPrice.HasValue)    variant.CompareAtPrice    = request.CompareAtPrice;
        if (request.DiscountStartDate.HasValue) variant.DiscountStartDate = request.DiscountStartDate;
        if (request.DiscountEndDate.HasValue)   variant.DiscountEndDate   = request.DiscountEndDate;
        if (request.IsActive.HasValue)          variant.IsActive          = request.IsActive.Value;

        if (request.Attributes is not null)
        {
            variant.AttributeValues.Clear();
            foreach (var attr in request.Attributes)
                variant.AttributeValues.Add(new VariantAttributeValue
                {
                    VariantId         = variant.Id,
                    AttributeOptionId = attr.AttributeOptionId
                });
        }

        await _variantRepository.UpdateAsync(variant);
        Log.Information("Variant updated: {VariantId}", variantId);

        if (variant.Quantity <= 5)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            await _lowStockHandler.HandleAsync(
                new LowStockEvent(variant.Id, variant.SKU, product!.MerchantId));
        }

        var updated = await _variantRepository.GetByIdWithAttributesAsync(variantId)
            ?? throw new InvalidOperationException("Failed to load updated variant.");

        return MapToResponse(updated);
    }

    public async Task DeleteAsync(Guid productId, Guid variantId)
    {
        await VerifyProductOwnershipAsync(productId);

        var variant = await _variantRepository.GetByIdAsync(variantId)
            ?? throw new KeyNotFoundException($"Variant '{variantId}' was not found.");

        if (variant.ProductId != productId)
            throw new KeyNotFoundException($"Variant '{variantId}' was not found.");

        await _variantRepository.DeleteAsync(variant);
        Log.Information("Variant deleted: {VariantId}", variantId);
    }

    private async Task VerifyProductOwnershipAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException($"Product '{productId}' was not found.");

        if (product.MerchantId != _currentMerchant.MerchantId)
            throw new UnauthorizedAccessException("You do not own this product.");
    }

    private static VariantResponse MapToResponse(ProductVariant v) => new()
    {
        Id                = v.Id,
        ProductId         = v.ProductId,
        SKU               = v.SKU,
        Quantity          = v.Quantity,
        LowStockThreshold = v.LowStockThreshold,
        PriceOverride     = v.PriceOverride,
        CompareAtPrice    = v.CompareAtPrice,
        DiscountStartDate = v.DiscountStartDate,
        DiscountEndDate   = v.DiscountEndDate,
        IsActive          = v.IsActive,
        CreatedAt         = v.CreatedAt,
        UpdatedAt         = v.UpdatedAt,
        Attributes        = v.AttributeValues.Select(av => new VariantAttributeResponse
        {
            AttributeOptionId = av.AttributeOptionId,
            AttributeName     = av.AttributeOption?.Attribute?.Name ?? string.Empty,
            OptionValue       = av.AttributeOption?.Value ?? string.Empty
        }).ToList()
    };
}
