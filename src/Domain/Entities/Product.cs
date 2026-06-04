using MultiVendor.Ecommerce.Domain.Common;
using MultiVendor.Ecommerce.Domain.Enums;

namespace MultiVendor.Ecommerce.Domain.Entities;

public class Product : BaseEntity
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public decimal BasePrice { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Merchant Merchant { get; set; } = null!;
    public virtual ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
