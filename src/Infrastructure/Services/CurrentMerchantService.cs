using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MultiVendor.Ecommerce.Application.Interfaces;

namespace MultiVendor.Ecommerce.Infrastructure.Services;

public class CurrentMerchantService : ICurrentMerchantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentMerchantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid MerchantId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("Merchant identity not found in token.");
        }
    }
}
