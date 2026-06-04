namespace MultiVendor.Ecommerce.Application.DTOs.Products;

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public decimal? BasePrice { get; set; }
}
