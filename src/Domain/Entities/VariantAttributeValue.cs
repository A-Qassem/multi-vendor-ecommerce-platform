namespace MultiVendor.Ecommerce.Domain.Entities;

/// <summary>
/// Join table between ProductVariant and AttributeOption.
/// Does NOT extend BaseEntity — uses a composite primary key (VariantId + AttributeOptionId)
/// </summary>
public class VariantAttributeValue
{
    public Guid VariantId { get; set; }
    public Guid AttributeOptionId { get; set; }

    // Navigation properties
    public virtual ProductVariant Variant { get; set; } = null!;
    public virtual AttributeOption AttributeOption { get; set; } = null!;
}
