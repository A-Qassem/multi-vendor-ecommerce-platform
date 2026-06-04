namespace MultiVendor.Ecommerce.Application.Events;

public record LowStockEvent(Guid VariantId, string SKU, Guid MerchantId);
