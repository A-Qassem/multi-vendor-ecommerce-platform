namespace MultiVendor.Ecommerce.Application.DTOs.Products;

public class ProductResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
