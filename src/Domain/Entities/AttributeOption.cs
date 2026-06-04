using MultiVendor.Ecommerce.Domain.Common;

namespace MultiVendor.Ecommerce.Domain.Entities;

public class AttributeOption : BaseEntity
{
    public Guid ProductAttributeId { get; set; }
    public string Value { get; set; } = string.Empty;

    // Navigation properties
    public virtual ProductAttribute Attribute { get; set; } = null!;
    public virtual ICollection<VariantAttributeValue> VariantAttributeValues { get; set; } = new List<VariantAttributeValue>();
}
