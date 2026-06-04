namespace MultiVendor.Ecommerce.Application.DTOs.Variants;

public class VariantAttributeResponse
{
    public Guid AttributeOptionId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string OptionValue { get; set; } = string.Empty;
}
