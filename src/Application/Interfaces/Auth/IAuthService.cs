using MultiVendor.Ecommerce.Application.DTOs.Auth;

namespace MultiVendor.Ecommerce.Application.Interfaces.Auth;


public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
}
