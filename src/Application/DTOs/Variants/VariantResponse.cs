namespace MultiVendor.Ecommerce.Application.DTOs.Variants;

public class VariantResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<VariantAttributeResponse> Attributes { get; set; } = [];
}
