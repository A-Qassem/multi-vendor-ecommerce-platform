namespace MultiVendor.Ecommerce.Application.DTOs.Products;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal BasePrice { get; set; }
}
