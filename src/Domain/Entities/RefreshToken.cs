using MultiVendor.Ecommerce.Domain.Common;

namespace MultiVendor.Ecommerce.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid MerchantId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    // Navigation properties
    public virtual Merchant Merchant { get; set; } = null!;
}
