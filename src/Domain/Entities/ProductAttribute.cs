using MultiVendor.Ecommerce.Domain.Common;

namespace MultiVendor.Ecommerce.Domain.Entities;

public class ProductAttribute : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<AttributeOption> Options { get; set; } = new List<AttributeOption>();
}
