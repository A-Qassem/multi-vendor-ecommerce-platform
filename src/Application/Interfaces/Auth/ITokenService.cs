using MultiVendor.Ecommerce.Domain.Entities;

namespace MultiVendor.Ecommerce.Application.Interfaces.Auth;

public interface ITokenService
{
    (string Token, DateTime Expiration) GenerateAccessToken(Merchant merchant);

    string GenerateRefreshToken();
}
