using MultiVendor.Ecommerce.Domain.Common;

namespace MultiVendor.Ecommerce.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}
