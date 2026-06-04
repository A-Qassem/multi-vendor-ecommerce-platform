namespace MultiVendor.Ecommerce.Application.DTOs.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }

    public Guid MerchantId { get; set; }
}
