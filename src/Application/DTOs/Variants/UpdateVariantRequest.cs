namespace MultiVendor.Ecommerce.Application.DTOs.Variants;

public class UpdateVariantRequest
{
    public string? SKU { get; set; }
    public int? Quantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public bool? IsActive { get; set; }
    public List<VariantAttributeRequest>? Attributes { get; set; }
}
