using MultiVendor.Ecommerce.Domain.Common;

namespace MultiVendor.Ecommerce.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public DateTime? DiscountStartDate { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<VariantAttributeValue> AttributeValues { get; set; } = new List<VariantAttributeValue>();
}
