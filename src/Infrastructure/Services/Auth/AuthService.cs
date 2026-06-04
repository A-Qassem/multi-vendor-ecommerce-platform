using Microsoft.EntityFrameworkCore;
using MultiVendor.Ecommerce.Application.DTOs.Auth;
using MultiVendor.Ecommerce.Application.Interfaces.Auth;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Infrastructure.Data;

namespace MultiVendor.Ecommerce.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var emailTaken = await _db.Merchants
            .AnyAsync(m => m.Email == request.Email, ct);

        if (emailTaken)
            throw new InvalidOperationException("Email is already registered.");

        var merchant = new Merchant
        {
            Name = request.Name,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _db.Merchants.AddAsync(merchant, ct);
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(merchant, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var merchant = await _db.Merchants
            .FirstOrDefaultAsync(m => m.Email == request.Email, ct);

        if (merchant is null || !BCrypt.Net.BCrypt.Verify(request.Password, merchant.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokensAsync(merchant, ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(Merchant merchant, CancellationToken ct)
    {
        var (accessToken, expiration) = _tokenService.GenerateAccessToken(merchant);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            MerchantId = merchant.Id,
            Token = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        await _db.RefreshTokens.AddAsync(refreshTokenEntity, ct);
        await _db.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            Expiration = expiration,
            MerchantId = merchant.Id
        };
    }
    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (token is null || token.IsRevoked)
            throw new InvalidOperationException("Refresh token is invalid or already revoked.");

        token.IsRevoked = true;
        await _db.SaveChangesAsync(ct);
    }
}
