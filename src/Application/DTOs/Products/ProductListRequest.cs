namespace MultiVendor.Ecommerce.Application.DTOs.Products;

public class ProductListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDir { get; set; } = "desc";
}
